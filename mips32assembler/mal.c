#include <stdlib.h>
#include <ctype.h>
#include <string.h>
#include <stdint.h>
#include <stdio.h>

///////////////////////////////////////////////////////////////////////////////
// Lexical
///////////////////////////////////////////////////////////////////////////////
enum TOKEN_IDS {
	TID_ADD,
	TID_SUB,
	TID_AND,
	TID_OR,
	TID_SLT,
	TID_SLL,
	TID_SRL,
	TID_SRA,
	TID_ADDI,
	TID_ANDI,
	TID_ORI,
	TID_LW,
	TID_SW,
	TID_BEQ,
	TID_BNE,
	TID_J,
	INSTRUCTION_COUNT,

	TID_TERMINATOR,

	TID_LABEL,
	TID_COLON,
	TID_COMMA,
	TID_NUMBER,
	TID_DOLLAR,
	TID_LPAREN,
	TID_RPAREN
};

const char *token_names[] = {
	"add", "sub", "and", "or", 
	"slt", "sll", "srl", "sra",
	"addi",	"andi", "ori",
	"lw", "sw",
	"beq", "bne", "j",
	NULL,

	"<terminator>",	"<label>", ":", ",",
	"<immediate>",	"$",	"(", ")"
};

struct token_t {
	int token_id;
	union {
		int immediate;
		char *text;
	} value;
} current;

FILE *input;
FILE *output;

enum DFA_STATES {
	START,
	IN_STRING,
	IN_NUMBER,
	IN_COMMENT,
	END
};

#define MAX_TOKEN_LENGTH 63

int lineno = 1;

void next_token()
{
	char buf[MAX_TOKEN_LENGTH + 1];
	int length = 0;
	int state = START;
	while (state != END)
	{
		int c = fgetc(input);
		switch (state)
		{
		case START:
			if (c == '\n')
			{
				lineno++;
				continue;
			}
			else if (isspace(c))
			{
				continue;
			}
			else if (isalpha(c))
			{
				buf[length++] = c;
				state = IN_STRING;
			}
			else if (c == '+' || c == '-' || isdigit(c))
			{
				buf[length++] = c;
				state = IN_NUMBER;
			}
			else if (c == '$')
			{
				current.token_id = TID_DOLLAR;
				state = END;
			}
			else if (c == '(')
			{
				current.token_id = TID_LPAREN;
				state = END;
			}
			else if (c == ')')
			{
				current.token_id = TID_RPAREN;
				state = END;
			}
			else if (c == ':')
			{
				current.token_id = TID_COLON;
				state = END;
			}
			else if (c == ',')
			{
				current.token_id = TID_COMMA;
				state = END;
			}
			else if (c == '#' || c == '\'')
			{
				state = IN_COMMENT;
			}
			else if (c == -1)
			{
				current.token_id = TID_TERMINATOR;
				state = END;
			}
			else
			{
				fprintf(stderr, "Error: Unknown character '%c' at line %d\n", c, lineno);
				exit(1);
			}
			break;

		case IN_STRING:
			if (isalnum(c))
			{
				buf[length++] = c;
			}
			else
			{
				// If meet end, DO NOT move back.
				if (c != -1)
					fseek(input, -1, SEEK_CUR);
				buf[length] = '\0';
				
				int i;
				for (i = 0; i < INSTRUCTION_COUNT; ++i)
				{
					if (!strcmp(buf, token_names[i]))
					{
						current.token_id = i;
						break;
					}
				}
				if (i == INSTRUCTION_COUNT)
				{ // It's a label.
					current.value.text = (char *)malloc(strlen(buf) + 1);
					strcpy(current.value.text, buf);
					current.token_id = TID_LABEL;
				}
				
				state = END;
			}
			break;

		case IN_NUMBER:
			if (isdigit(c))
			{
				buf[length++] = c;
			}
			else
			{
				// If meet end, DO NOT move back.
				if (c != -1)
					fseek(input, -1, SEEK_CUR);
				buf[length] = '\0';
				current.token_id = TID_NUMBER;
				current.value.immediate = atoi(buf);
				state = END;
			}
			break;

		case IN_COMMENT:
			if (c == '\n')
			{
				lineno++;
				state = START;
			}
			else if (c == -1)
			{
				current.token_id = TID_TERMINATOR;
				state = END;
			}
			break;
		}
	}
}

void eat(int token_id)
{
	if (token_id != current.token_id)
	{
		fprintf(stderr, "Error: Token '%s' expected at line %d\n", token_names[token_id], lineno);
		exit(1);
	}
	next_token();
}

///////////////////////////////////////////////////////////////////////////////
// Checked Return
///////////////////////////////////////////////////////////////////////////////
#define MIN_REGNO 0
#define MAX_REGNO 31

int checked_return_regno()
{
	int temp = current.value.immediate;
	
	int old_lineno = lineno;
	eat(TID_NUMBER);

	if (temp < MIN_REGNO || temp > MAX_REGNO)
	{
		fprintf(stderr, "Error: Reg.NO '%d' out of range at line %d\n", temp, old_lineno);
		exit(1);
	}

	return temp;
}

int checked_return_immediate()
{
	int temp = current.value.immediate;

	int old_lineno = lineno;
	eat(TID_NUMBER);

	if (temp < INT16_MIN || temp > INT16_MAX)
	{
		fprintf(stderr, "Error: Immediate '%d' out of range at line %d\n", temp, old_lineno);
		exit(1);
	}

	return temp;
}

#define MIN_SHAMT 0
#define MAX_SHAMT 31

int checked_return_shamt()
{
	int temp = current.value.immediate;
	
	int old_lineno = lineno;
	eat(TID_NUMBER);

	if (temp < MIN_SHAMT || temp > MAX_SHAMT)
	{
		fprintf(stderr, "Error: Shift amount '%d' out of range at line %d\n", temp, old_lineno);
		exit(1);
	}

	return temp;
}

#define INT26_MIN (-(1 << 25))
#define INT26_MAX ((1 << 25) - 1)

int checked_return_address()
{
	int temp = current.value.immediate;

	int old_lineno = lineno;
	eat(TID_NUMBER);

	if (temp < INT26_MIN || temp > INT26_MAX)
	{
		fprintf(stderr, "Error: Address '%08X' out of range at line %d\n", temp, old_lineno);
		exit(1);
	}

	return temp;
}

///////////////////////////////////////////////////////////////////////////////
// Instruction related
///////////////////////////////////////////////////////////////////////////////
unsigned Op_codes[] = {
	0,	0,	0,	0, 
	0,	0,	0,	0,
	8,	12,	13,	35,
	43,	4,	5,	2
};

unsigned Opx_codes[] = {
	32,	34,	36,	37,
	42,	0,	2,	3
};

typedef union {
	unsigned inst;

	struct {
		unsigned Opx : 6;
		unsigned Shamt : 5;
		unsigned Rd : 5;
		unsigned Rt : 5;
		unsigned Rs : 5;
		unsigned Op : 6;
	} RR_type;

	struct {
		unsigned immediate : 16;
		unsigned Rt : 5; // Also known as Rd in Branch
		unsigned Rs : 5;
		unsigned Op : 6;
	} RI_type;

	struct {
		unsigned target : 26;
		unsigned Op : 6;
	} J_type;
} instruction_t;

#define MAX_INSTRUCTION_COUNT 1024
#define INSTRUCTION_SIZE_IN_BYTES 4

// Element 0 is reserved for empty 'pointer'.
instruction_t ibuf[(MAX_INSTRUCTION_COUNT + 1) * INSTRUCTION_SIZE_IN_BYTES];
int ibuf_top = 1;

///////////////////////////////////////////////////////////////////////////////
// Label linkage
///////////////////////////////////////////////////////////////////////////////
typedef struct map_entry_tag {
	char *label_name;
	int address;
	int top_inst;
	struct map_entry_tag *next_entry;
} map_entry_t;

map_entry_t head;

map_entry_t *add_label(const char *name)
{
	map_entry_t *p = (map_entry_t *)malloc(sizeof(map_entry_t));
	p->address = -1;
	p->label_name = (char *)malloc(strlen(name) + 1);
	strcpy(p->label_name, name);
	p->next_entry = head.next_entry;
	p->top_inst = 0;
	head.next_entry = p;

	return p;
}

map_entry_t *lookup_label(const char *name)
{
	map_entry_t *p = head.next_entry;
	while (p != NULL)
	{
		if (!strcmp(p->label_name, name))
			return p;
		p = p->next_entry;
	}

	return NULL;
}

int get_branch_address(int target, int curr)
{
	return target - curr - 1;
}

int get_jump_address(int target, int curr)
{
	// DO NOT forget element 0 is reserved.
	return target - 1;
}

void backpatch()
{
	map_entry_t *p = head.next_entry;
	while (p != NULL)
	{
		if (p->address == -1)
		{
			fprintf(stderr, "Error: Label '%s' undefined\n", p->label_name);
			exit(1);
		}

		int curr_inst = p->top_inst;
		while (curr_inst != 0)
		{
			int next_inst = 0;
			if (ibuf[curr_inst].J_type.Op == Op_codes[TID_BEQ] || ibuf[curr_inst].J_type.Op == Op_codes[TID_BNE])
			{
				next_inst = ibuf[curr_inst].RI_type.immediate;
				ibuf[curr_inst].RI_type.immediate = get_branch_address(p->address, curr_inst);
			}
			else if (ibuf[curr_inst].J_type.Op == Op_codes[TID_J])
			{
				next_inst = ibuf[curr_inst].J_type.target;
				ibuf[curr_inst].J_type.target = get_jump_address(p->address, curr_inst);
			}
			else
			{
				fprintf(stderr, "Error: Unexpected code in link\n");
				exit(1);
			}

			curr_inst = next_inst;
		}

		p = p->next_entry;
	}
}

///////////////////////////////////////////////////////////////////////////////
// Parsing, code generating and backpatch
///////////////////////////////////////////////////////////////////////////////
void parse_codegen()
{
	next_token();
	while (current.token_id != TID_TERMINATOR)
	{
		map_entry_t *entry;
		if (current.token_id == TID_LABEL)
		{
			entry = lookup_label(current.value.text);
			if (entry != NULL && entry->address != -1)
			{ // Label already defined
				fprintf(stderr, "Error: Label '%s' redefinition at line %d\n", entry->label_name, lineno);
				exit(1);
			}
			if (entry == NULL)
				entry = add_label(current.value.text);
			entry->address = ibuf_top;

			free(current.value.text);

			next_token();
			eat(TID_COLON);
		}

		instruction_t inst;
		switch (current.token_id)
		{
		// R-Type
		case TID_ADD:
		case TID_SUB:
		case TID_AND:
		case TID_OR:
		case TID_SLT:
			inst.RR_type.Op = Op_codes[current.token_id];
			inst.RR_type.Opx = Opx_codes[current.token_id];
			next_token();
		
			// Rd
			eat(TID_DOLLAR);
			inst.RR_type.Rd = checked_return_regno();

			eat(TID_COMMA);

			// Rs
			eat(TID_DOLLAR);
			inst.RR_type.Rs = checked_return_regno();

			eat(TID_COMMA);

			// Rt
			eat(TID_DOLLAR);
			inst.RR_type.Rt = checked_return_regno();
			
			inst.RR_type.Shamt = 0;
			break;

		case TID_SLL:
		case TID_SRL:
		case TID_SRA:
			inst.RR_type.Op = Op_codes[current.token_id];
			inst.RR_type.Opx = Opx_codes[current.token_id];
			next_token();

			inst.RR_type.Rs = 0;
		
			// Rd
			eat(TID_DOLLAR);
			inst.RR_type.Rd = checked_return_regno();

			eat(TID_COMMA);

			// Rt
			eat(TID_DOLLAR);
			inst.RR_type.Rt = checked_return_regno();

			eat(TID_COMMA);

			// Shamt
			inst.RR_type.Shamt = checked_return_shamt();
			
			break;

		// I-Type
		case TID_ADDI:
		case TID_ANDI:
		case TID_ORI:
			inst.RI_type.Op = Op_codes[current.token_id];
			next_token();

			// Rt.
			eat(TID_DOLLAR);
			inst.RI_type.Rt = checked_return_regno();

			eat(TID_COMMA);

			// Rs
			eat(TID_DOLLAR);
			inst.RI_type.Rs = checked_return_regno();

			eat(TID_COMMA);

			// Immediate
			inst.RI_type.immediate = checked_return_immediate();
			break;
		
		case TID_BEQ:
		case TID_BNE:
			inst.RI_type.Op = Op_codes[current.token_id];
			next_token();

			// Rs
			eat(TID_DOLLAR);
			inst.RI_type.Rs = checked_return_regno();

			eat(TID_COMMA);

			// Rt
			eat(TID_DOLLAR);
			inst.RI_type.Rt = checked_return_regno();

			eat(TID_COMMA);

			// Target
			if (current.token_id == TID_LABEL)
			{
				entry = lookup_label(current.value.text);
				if (entry == NULL)
				{
					entry = add_label(current.value.text);
					entry->top_inst = ibuf_top;
					inst.RI_type.immediate = 0;
				}
				else
				{
					if (entry->top_inst > INT16_MAX)
					{
						fprintf(stderr, "Error: Too many codes\n");
						exit(1);
					}

					if (entry->address == -1)
					{ // Label not defined yet
						inst.RI_type.immediate = entry->top_inst;
						entry->top_inst = ibuf_top;
					}
					else
					{
						inst.RI_type.immediate =  get_branch_address(entry->address, ibuf_top);
					}
				}

				free(current.value.text);
				next_token();
			}
			else
			{
				inst.RI_type.immediate = checked_return_immediate();
			}
			break;

		case TID_LW:
		case TID_SW:
			inst.RI_type.Op = Op_codes[current.token_id];
			next_token();

			// Rt.
			eat(TID_DOLLAR);
			inst.RI_type.Rt = checked_return_regno();

			eat(TID_COMMA);

			// Offset.
			inst.RI_type.immediate = checked_return_immediate();

			eat(TID_LPAREN);

			// Rs
			eat(TID_DOLLAR);
			inst.RI_type.Rs = checked_return_regno();

			eat(TID_RPAREN);
			break;

		// J-Type
		case TID_J:
			inst.RI_type.Op = Op_codes[current.token_id];
			next_token();

			// Address.
			if (current.token_id == TID_LABEL)
			{
				entry = lookup_label(current.value.text);
				if (entry == NULL)
				{
					entry = add_label(current.value.text);
					entry->top_inst = ibuf_top;
					inst.J_type.target = 0;
				}
				else
				{
					if (entry->top_inst > INT26_MAX)
					{
						fprintf(stderr, "Error: Too many codes\n");
						exit(1);
					}

					if (entry->address == -1)
					{ // Label not defined yet
						inst.J_type.target = entry->top_inst;
						entry->top_inst = ibuf_top;
					}
					else
					{
						inst.J_type.target = get_jump_address(entry->address, ibuf_top);
					}
				}

				free(current.value.text);
				next_token();
			}
			else
			{
				inst.J_type.target = checked_return_address();
			}
			break;

		default:
			fprintf(stderr, "Error: Unknown opcode\n");
			exit(1);
			break;
		}

		ibuf[ibuf_top++] = inst;
	}
}

int main(int argc, char* argv[])
{
	if (argc != 2)
	{
		printf("Tiny MIPS32 Assembler v0.1\nAuthor: Lewis Cheng\nUpdated: 2011/10/11\n\nUsage: %s <filename>\n", argv[0]);
		return 0;
	}

	input = fopen(argv[1], "r");
	if (input == NULL)
	{
		fprintf(stderr, "Error: Cannot open file '%s'\n", argv[1]);
		return 1;
	}
	
	parse_codegen();
	fclose(input);
	backpatch();

	// Output to screen
	printf("\nMachine code generated for '%s':\n", argv[1]);
	int i;
	for (i = 1; i < ibuf_top; ++i)
	{
		// Original style.
		//int address = (i - 1) << 2;
		//printf("%08X:  %08X\n", address, ibuf[i]);
		
		// Verilog style.
		//printf("\t\tROM[%d] <= 32\'h%08x;\n", i - 1, ibuf[i]);
		
		// C* style.
		printf("\t\t0x%08x,\n", ibuf[i]);
	}

	return 0;
}
