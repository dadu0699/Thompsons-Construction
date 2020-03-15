﻿using OCL1P1.model;
using OCL1P1.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCL1P1.analyzer
{
    class Interpreter
    {
        private int index;
        private Token preAnalysis;
        private bool syntacticError;
        private int idError;
        private int idSymbol;

        internal List<Token> ListToken { get; set; }
        internal List<Error> ListError { get; set; }
        internal List<Symbol> SymbolTable { get; set; }

        public Interpreter(List<Token> listToken)
        {
            ListToken = listToken;
            ListToken.Add(new Token(0, 0, 0, Token.Type.END, "END"));
            index = 0;
            preAnalysis = ListToken[index];
            syntacticError = false;

            idError = 0;
            idSymbol = 0;
            ListError = new List<Error>();
            SymbolTable = new List<Symbol>();

            Start();
        }

        private void Start()
        {
            INSTP();
        }

        private void INSTP()
        {
            if (preAnalysis.TypeToken == Token.Type.RESERVED_CONJ
                || preAnalysis.TypeToken == Token.Type.ID
                || preAnalysis.TypeToken == Token.Type.COMMENT
                || preAnalysis.TypeToken == Token.Type.MULTILINE_COMMENT)
            {
                INST();
                INSTP();
            }
        }

        private void INST()
        {
            if (preAnalysis.TypeToken == Token.Type.RESERVED_CONJ)
            {
                DEFCONJ();
            }
            else if (preAnalysis.TypeToken == Token.Type.ID)
            {
                EXPR();
            }
            else if (preAnalysis.TypeToken == Token.Type.COMMENT)
            {
                Parser(Token.Type.COMMENT);
            }
            else if (preAnalysis.TypeToken == Token.Type.MULTILINE_COMMENT)
            {
                Parser(Token.Type.MULTILINE_COMMENT);
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'DEFINITION " +
                    "| EXPRESSION | COMMENT | MULTILINE COMMENT'");
                syntacticError = true;
            }
        }

        private void DEFCONJ()
        {
            if (preAnalysis.TypeToken == Token.Type.RESERVED_CONJ)
            {
                string type = preAnalysis.Value;
                Parser(Token.Type.RESERVED_CONJ);
                Parser(Token.Type.SYMBOL_COLON);
                string name = preAnalysis.Value;
                Parser(Token.Type.ID);
                Parser(Token.Type.ASSIGNMENT_SIGN);
                List<Token> value = ASGMTCONJ();
                Parser(Token.Type.SYMBOL_SEMICOLON);
                AddSymbol(type, name, value);
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'CONJ'");
            }
        }

        private List<Token> ASGMTCONJ()
        {
            List<Token> tokens = new List<Token>();
            if (preAnalysis.TypeToken == Token.Type.ID
                || preAnalysis.TypeToken == Token.Type.NUMBER
                || preAnalysis.TypeToken == Token.Type.SYMBOL)
            {
                tokens.Add(TYPECONJ());
                tokens.AddRange(ASGMTCONJP());
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'ID | Number | Symbol'");
            }
            return tokens;
        }

        private List<Token> ASGMTCONJP()
        {
            List<Token> tokens = new List<Token>();
            if (preAnalysis.TypeToken == Token.Type.SET_SIGN)
            {
                tokens.Add(preAnalysis);
                Parser(Token.Type.SET_SIGN);
                tokens.AddRange(ASGMTCONJ());
            }
            else if (preAnalysis.TypeToken == Token.Type.SYMBOL_COMMA)
            {
                tokens.Add(preAnalysis);
                Parser(Token.Type.SYMBOL_COMMA);
                tokens.AddRange(ASGMTCONJ());
            }
            return tokens;
        }

        private Token TYPECONJ()
        {
            Token token = null;
            if (preAnalysis.TypeToken == Token.Type.ID)
            {
                token = preAnalysis;
                Parser(Token.Type.ID);
            }
            else if (preAnalysis.TypeToken == Token.Type.NUMBER)
            {
                token = preAnalysis;
                Parser(Token.Type.NUMBER);
            }
            else if (preAnalysis.TypeToken == Token.Type.SYMBOL)
            {
                token = preAnalysis;
                Parser(Token.Type.SYMBOL);
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'ID | Number | Symbol'");
            }
            return token;
        }

        private void EXPR()
        {
            if (preAnalysis.TypeToken == Token.Type.ID)
            {
                Parser(Token.Type.ID);
                OPTEXPR();
                Parser(Token.Type.SYMBOL_SEMICOLON);
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'ID'");
            }
        }

        private void OPTEXPR()
        {
            if (preAnalysis.TypeToken == Token.Type.ASSIGNMENT_SIGN)
            {
                string type = "EXPR";
                string name = ListToken[index-1].Value;
                AddSymbol(type, name, null);

                DEFEXPR();
            }
            else if (preAnalysis.TypeToken == Token.Type.SYMBOL_COLON)
            {
                VALEXPR();
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'ASSIGNMENT SIGN | SYMBOL COLON'");
            }
        }

        private void DEFEXPR()
        {
            if (preAnalysis.TypeToken == Token.Type.ASSIGNMENT_SIGN)
            {
                Symbol symbol = GetSymbol(ListToken[index - 1].Value);
                Parser(Token.Type.ASSIGNMENT_SIGN);
                symbol.Value = STRUCEXPR();
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'ID'");
            }
        }

        private List<Token> STRUCEXPR()
        {
            List<Token> tokens = new List<Token>();
            if (preAnalysis.TypeToken == Token.Type.CONCATENATION_SIGN
                || preAnalysis.TypeToken == Token.Type.DISJUNCTION_SIGN
                || preAnalysis.TypeToken == Token.Type.QUESTION_MARK_SIGN
                || preAnalysis.TypeToken == Token.Type.ASTERISK_SIGN
                || preAnalysis.TypeToken == Token.Type.PLUS_SIGN
                || preAnalysis.TypeToken == Token.Type.SYMBOL_LEFT_CURLY_BRACKET
                || preAnalysis.TypeToken == Token.Type.STR)
            {
                tokens.Add(SYMBEXPR());
                tokens.AddRange(STRUCEXPRP());
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'CONCATENATION SIGN " +
                    "| DISJUNCTION SIGN | QUESTION MARK SIGN | ASTERISK SIGN | PLUS SIGN | SYMBOL LEFT CURLY BRACKET " +
                    "| STRING'");
            }
            return tokens;
        }

        private List<Token> STRUCEXPRP()
        {
            List<Token> tokens = new List<Token>();
            if (preAnalysis.TypeToken == Token.Type.CONCATENATION_SIGN
                || preAnalysis.TypeToken == Token.Type.DISJUNCTION_SIGN
                || preAnalysis.TypeToken == Token.Type.QUESTION_MARK_SIGN
                || preAnalysis.TypeToken == Token.Type.ASTERISK_SIGN
                || preAnalysis.TypeToken == Token.Type.PLUS_SIGN
                || preAnalysis.TypeToken == Token.Type.SYMBOL_LEFT_CURLY_BRACKET
                || preAnalysis.TypeToken == Token.Type.STR)
            {
                tokens.Add(SYMBEXPR());
                tokens.AddRange(STRUCEXPRP());
            }
            return tokens;
        }

        private Token SYMBEXPR()
        {
            Token token = null;
            if (preAnalysis.TypeToken == Token.Type.CONCATENATION_SIGN)
            {
                token = preAnalysis;
                Parser(Token.Type.CONCATENATION_SIGN);
            }
            else if (preAnalysis.TypeToken == Token.Type.DISJUNCTION_SIGN)
            {
                token = preAnalysis;
                Parser(Token.Type.DISJUNCTION_SIGN);
            }
            else if (preAnalysis.TypeToken == Token.Type.QUESTION_MARK_SIGN)
            {
                token = preAnalysis;
                Parser(Token.Type.QUESTION_MARK_SIGN);
            }
            else if (preAnalysis.TypeToken == Token.Type.ASTERISK_SIGN)
            {
                token = preAnalysis;
                Parser(Token.Type.ASTERISK_SIGN);
            }
            else if (preAnalysis.TypeToken == Token.Type.PLUS_SIGN)
            {
                token = preAnalysis;
                Parser(Token.Type.PLUS_SIGN);
            }
            else if (preAnalysis.TypeToken == Token.Type.SYMBOL_LEFT_CURLY_BRACKET)
            {
                Parser(Token.Type.SYMBOL_LEFT_CURLY_BRACKET);
                token = preAnalysis;
                Parser(Token.Type.ID);
                Parser(Token.Type.SYMBOL_RIGHT_CURLY_BRACKET);
            }
            else if (preAnalysis.TypeToken == Token.Type.STR)
            {
                token = preAnalysis;
                Parser(Token.Type.STR);
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'CONCATENATION SIGN " +
                    "| DISJUNCTION SIGN | QUESTION MARK SIGN | ASTERISK SIGN | PLUS SIGN | SYMBOL LEFT CURLY BRACKET " +
                    "| STRING'");
            }
            return token;
        }

        private void VALEXPR()
        {
            if (preAnalysis.TypeToken == Token.Type.SYMBOL_COLON)
            {
                Parser(Token.Type.SYMBOL_COLON);
                Parser(Token.Type.STR);
            }
            else
            {
                AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected 'ID'");
            }
        }

        private void Parser(Token.Type type)
        {
            if (syntacticError)
            {
                if (index < ListToken.Count - 1)
                {
                    index++;
                    preAnalysis = ListToken[index];
                    if (preAnalysis.TypeToken == Token.Type.SYMBOL_SEMICOLON)
                    {
                        syntacticError = false;
                    }
                }
            }
            else
            {
                if (preAnalysis.TypeToken != Token.Type.END)
                {
                    if (preAnalysis.TypeToken == type)
                    {
                        index++;
                        preAnalysis = ListToken[index];
                    }
                    else
                    {
                        AddError(preAnalysis.Row, preAnalysis.Column, preAnalysis.toStringTypeToken, "Was expected '" + type + "'");
                        syntacticError = true;
                    }
                }
            }
        }

        private void AddError(int row, int column, string str, string description)
        {
            idError++;
            ListError.Add(new Error(idError, row, column, str, description));
            syntacticError = true;
        }

        public void AddSymbol(string type, string name, List<Token> value)
        {
            idSymbol++;
            SymbolTable.Add(new Symbol(idSymbol, type, name, value));
        }

        public Symbol GetSymbol(string name)
        {
            foreach (Symbol symbol in SymbolTable)
            {
                if (symbol.Name.Equals(name))
                {
                    return symbol;
                }
            }
            return null;
        }

        public void GenerateReports()
        {
            XMLReport xmlReport = new XMLReport();
            xmlReport.ReportSymbolTable(SymbolTable);
        }
    }
}