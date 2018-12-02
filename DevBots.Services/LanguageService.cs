using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Xsl;
using DevBots.Services.Interfaces;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Internal;

namespace DevBots.Services
{
    public class LanguageService : ILanguageService
    {
        private Dictionary<string, string> _variables;
        private Dictionary<string, string> _types;
        private const string ConsoleDebug = "DEBUG INFO: ";
        public LanguageService()
        {
        }

        public Responses<RobotCommand> Decode(string scriptPath)
        {
            var result = new Responses<RobotCommand>();
            _variables = new Dictionary<string, string>();
            _types = new Dictionary<string, string>();
            scriptPath = Path.GetFullPath(scriptPath);
            if (!File.Exists(scriptPath))
            {
                result.Errors.Add("Something went wrong. This script does not exist.");
                return result;
            }

            var script = File.ReadAllText(scriptPath);
            var tokens = Lexer(script);
            result.Model = Parser(tokens).ToList();
            return result;
        }

        private IEnumerable<RobotCommand> Parser(IEnumerable<Token> tokens)
        {
            var result = new List<RobotCommand>();
            var tokenGroups = new List<List<Token>>();
            var tokenGroup = new List<Token>();
            var expectEndIf = 0;
            foreach (var token in tokens)
            {
                if (token.Type == Types.IF)
                {
                    expectEndIf++;
                }
                else if (token.Type == Types.ENDIF)
                {
                    expectEndIf--;
                    if (expectEndIf < 0)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = "An ENDIF can't be before an IF"
                        });
                        return result;
                    }
                }
                if (token.Type != Types.NEWLINE)
                {
                    tokenGroup.Add(token);
                }
                else
                {
                    tokenGroups.Add(tokenGroup);
                    tokenGroup = new List<Token>();
                }
            }

            if (expectEndIf != 0)
            {
                result.Add(new RobotCommand
                {
                    Error = "Each IF should end with and ENDIF"
                });
                return result;
            }

            var shouldDo = new List<bool> {true};

            foreach (var tGroup in tokenGroups)
            {
                if (tGroup.ElementAt(0).Type == Types.IF)
                {
                    shouldDo.Add(shouldDo.Last());
                } 
                switch (tGroup.Count)
                {
                    case 1 when tGroup.First().Type == Types.ENDIF:
                        shouldDo.RemoveAt(shouldDo.Count - 1);
                        continue;
                    case 1 when tGroup.First().Type == Types.ELSE:
                        var last = shouldDo.Last();
                        shouldDo.RemoveAt(shouldDo.Count - 1);
                        shouldDo.Add(!last);
                        continue;
                }

                if (shouldDo.Any(s => !s))
                {
                    continue;
                }
                var index = 0;
                var groupLength = tGroup.Count;
                _print("Token Group-----------------------------------");
                while (index < tGroup.Count)
                {
                    _print(tGroup.ElementAt(index));
                    index++;
                }

                if (tGroup.ElementAt(0).Type == Types.IF && tGroup.Last().Type == Types.THEN)
                {
                    var conditions = tGroup.GetRange(1, tGroup.Count - 2);
                    if (conditions.Count == 1)
                    {
                        var condition = conditions.First();
                        if (condition.Type == Types.BOOLEAN)
                        {
                            shouldDo.RemoveAt(shouldDo.Count - 1);
                            shouldDo.Add(condition.Value == "TRUE");
                        }
                        else if (condition.Type == Types.UNDEFINED)
                        {
                            _variables.TryGetValue(condition.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"Undefined variable at line {condition.LineNumber}"
                                });
                                return result;
                            }

                            if (_types[condition.Value] == "BOOLEAN")
                            {
                                shouldDo.RemoveAt(shouldDo.Count - 1);
                                shouldDo.Add(value == "TRUE");
                            }
                            else
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"At line {condition.LineNumber}. IF statement accepts only BOOLEAN"
                                });
                                return result;
                            }
                        }
                        else
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"At line {condition.LineNumber}. IF statement accepts only BOOLEAN"
                            });
                            return result;
                        }
                    }
                    else
                    {
                        if (conditions.Any(c => c.Type == Types.COMPARE))
                        {
                            var res = _resolveBoolean(conditions);
                            if (res == null)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"An error occured at line {conditions.First().LineNumber}"
                                });
                                return result;
                            }
                            shouldDo.RemoveAt(shouldDo.Count - 1);
                            shouldDo.Add(res == true);
                        }
                    }
                }
                else if (tGroup.ElementAt(0).Type == Types.PRINT)
                {
                    if (groupLength == 2)
                    {
                        if (tGroup.ElementAt(1).Type == Types.EXPRESSION)
                        {
                            result.Add(new RobotCommand
                            {
                                Console = _evaluateExpression(tGroup.ElementAt(1).Value).ToString(),
                            });
                        }
                        else
                        {
                            if (tGroup.ElementAt(1).Type == Types.NUMBER || tGroup.ElementAt(1).Type == Types.STRING)
                            {
                                result.Add(new RobotCommand
                                {
                                    Console = tGroup.ElementAt(1).Value
                                });
                            }
                            else if (tGroup.ElementAt(1).Type == Types.UNDEFINED)
                            {
                                _variables.TryGetValue(tGroup.ElementAt(1).Value, out var value);
                                if (value == null)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"Variable {tGroup.ElementAt(1).Value} is unassigned"
                                    });
                                    return result;
                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Console = value,
                                    });
                                }
                            }
                            else
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"Type {tGroup.ElementAt(1).Type.ToString()} is not a valid type at line {tGroup.ElementAt(0).LineNumber}",
                                });
                                return result;
                            }
                        }
                    }
                    else
                    {
                        var nextIsNot = false;
                        tGroup.GetRange(1, tGroup.Count - 1).ForEach(t =>
                        {
                            if (t.Type == Types.NOT)
                            {
                                nextIsNot = true;
                                return;
                            }
                            if (t.Type != Types.UNDEFINED) return;
                            _variables.TryGetValue(t.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand()
                                {
                                    Error = $"An error occured in line {t.LineNumber}, unknown value {t.Value}",
                                });
                            }
                            else
                            {
                                t.Type = _types[t.Value] == "STRING" ? Types.STRING : (_types[t.Value] == "NUMBER" ? Types.NUMBER : Types.BOOLEAN);
                                t.Value = value;
                                if (!nextIsNot) return;
                                if (t.Type == Types.BOOLEAN)
                                {
                                    t.Value = t.Value == "TRUE" ? "FALSE" : "TRUE";
                                    nextIsNot = false;
                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {t.LineNumber} NOT can be used on BOOLEAN only"
                                    });
                                }
                            }
                        });
                        if (tGroup.GetRange(1, tGroup.Count - 1).Any(t => t.Type == Types.UNDEFINED) || nextIsNot)
                        {
                            if (nextIsNot)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"At line {tGroup.ElementAt(0).LineNumber} NOT can be used on BOOLEAN only"
                                });
                            }
                            return result;
                        }
                        else if (tGroup.Count(t => t.Type == Types.COMPARE) == 1)
                        {
                            var boolean = _resolveBoolean(tGroup.GetRange(1, tGroup.Count - 1));
                            switch (boolean)
                            {
                                case null:
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"An error occured in line {tGroup.ElementAt(0).LineNumber}"
                                    });
                                    return result;
                                case true:
                                    result.Add(new RobotCommand()
                                    {
                                        Console = "TRUE",
                                    });
                                    break;
                                case false:
                                    result.Add(new RobotCommand()
                                    {
                                        Console = "FALSE",
                                    });
                                    break;
                            }
                        }
                        else if (tGroup.Count(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER) == tGroup.Count - 1)
                        {
                            var res = _evaluateExpression(tGroup
                                .Where(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER).Select(t => t.Value)
                                .Join(""));
                            result.Add(new RobotCommand()
                            {
                                Console = res.ToString(),
                            });
                        }
                        else if (tGroup.Count(t => t.Type == Types.STRING || t.Type == Types.NUMBER || t.Type == Types.EXPRESSION || t.Type == Types.BOOLEAN || t.Type == Types.NOT) ==
                            tGroup.Count - 1)
                        {
                            if (tGroup.Where(t => t.Type == Types.EXPRESSION).All(t => t.Value == "+"))
                            {
                                var text = tGroup.Where(t => t.Type == Types.NUMBER || t.Type == Types.STRING || t.Type == Types.BOOLEAN).Select(t => t.Value)
                                    .Join("");
                                result.Add(new RobotCommand
                                {
                                    Console = text,
                                    CountsAsCommand = false,
                                });
                            }
                            else
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"An error occured in line {tGroup.ElementAt(0).LineNumber}"
                                });
                                return result;
                            }
                        }
                        else
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}"
                            });
                            return result;
                        }
                    }
                }
                else if (tGroup.ElementAt(0).Type == Types.LET)
                {
                    if (tGroup.ElementAt(1).Type == Types.UNDEFINED && tGroup.ElementAt(2).Type == Types.EQUALS)
                    {
                        var nextIsNot = false;
                        tGroup.GetRange(3, tGroup.Count - 3).ForEach(t =>
                        {
                            if (t.Type == Types.NOT)
                            {
                                nextIsNot = true;
                                return;
                            }
                            if (t.Type != Types.UNDEFINED) return;
                            _variables.TryGetValue(t.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand()
                                {
                                    Error = $"An error occured in line {t.LineNumber}",
                                });
                            }
                            else
                            {
                                t.Type = _types[t.Value] == "STRING" ? Types.STRING : (_types[t.Value] == "NUMBER" ? Types.NUMBER : Types.BOOLEAN);
                                t.Value = value;
                                if (!nextIsNot) return;
                                if (t.Type == Types.BOOLEAN)
                                {
                                    t.Value = t.Value == "TRUE" ? "FALSE" : "TRUE";
                                    nextIsNot = false;
                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {t.LineNumber} NOT can be used on BOOLEAN only"
                                    });
                                    t.Type = Types.UNDEFINED;
                                }
                            }
                        });
                        if (tGroup.GetRange(3, tGroup.Count - 3).Any(t => t.Type == Types.UNDEFINED) || nextIsNot)
                        {
                            if (nextIsNot)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"At line {tGroup.ElementAt(0).LineNumber} NOT can be used on BOOLEAN only"
                                });
                            }
                        }
                        else if ((tGroup.Count == 4 || tGroup.Count == 5) && (tGroup.Count(t => t.Type == Types.BOOLEAN) == 1 || tGroup.Count(t => t.Type == Types.BOOLEAN || t.Type == Types.NOT) == 2))
                        {
                            if (tGroup.FirstOrDefault(t => t.Type == Types.BOOLEAN) == null)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}"
                                });
                            }
                            else
                            {
                                _variables[tGroup.ElementAt(1).Value] = tGroup.FirstOrDefault(t => t.Type == Types.BOOLEAN)?.Value;
                                _types[tGroup.ElementAt(1).Value] = "BOOLEAN";
                            }
                        }
                        else if (tGroup.Count(t => t.Type == Types.COMPARE) == 1)
                        {
                            var boolean = _resolveBoolean(tGroup.GetRange(3, tGroup.Count - 3));
                            switch (boolean)
                            {
                                case null:
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"An error occured in line {tGroup.ElementAt(0).LineNumber}"
                                    });
                                    break;
                                case true:
                                    _variables[tGroup.ElementAt(1).Value] = "TRUE";
                                    _types[tGroup.ElementAt(1).Value] = "BOOLEAN";
                                    break;
                                case false:
                                    _variables[tGroup.ElementAt(1).Value] = "FALSE";
                                    _types[tGroup.ElementAt(1).Value] = "BOOLEAN";
                                    break;
                            }
                        }
                        else if (tGroup.Count(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER) == tGroup.Count - 3)
                        {
                            var res = _evaluateExpression(tGroup
                                .Where(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER).Select(t => t.Value)
                                .Join(""));
                            _variables[tGroup.ElementAt(1).Value] = res.ToString();
                            _types[tGroup.ElementAt(1).Value] = "NUMBER";
                        }
                        else if (tGroup.Count(t => t.Type == Types.STRING || t.Type == Types.NUMBER || t.Type == Types.EXPRESSION || t.Type == Types.BOOLEAN || t.Type == Types.NOT) ==
                                 tGroup.Count - 3)
                        {
                            if (tGroup.Where(t => t.Type == Types.EXPRESSION).All(t => t.Value == "+"))
                            {
                                var text = tGroup.Where(t => t.Type == Types.NUMBER || t.Type == Types.BOOLEAN || t.Type == Types.STRING).Select(t => t.Value)
                                    .Join("");
                                _variables[tGroup.ElementAt(1).Value] = text;
                                _types[tGroup.ElementAt(1).Value] = "STRING";
                            }
                            else
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"An error occured in line {tGroup.ElementAt(0).LineNumber}"
                                });
                            }
                        }
                    }
                    else
                    {
                        result.Add(new RobotCommand()
                        {
                            Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}, after LET expected 'variableName = value'",
                        });
                    }
                }
                else
                {
                    result.Add(new RobotCommand
                    {
                        Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}"
                    });
                }
            }
            return result;
        }

        private IEnumerable<Token> Lexer(string script)
        {
            //script += "<EOF>";
            var lineNumber = 1;
            var tokens = new List<Token>();
            var isString = false;
            var text = "";
            script = script.Replace("\t", "");
            var lines = script.Split("\r\n");
            foreach (var line in lines)
            {
                //_print(line);
                foreach (var word in line.Split(' '))
                {
                    if (word == "")
                    {
                        continue;
                    }
                    //_print(word);
                    if (isString && !word.Contains("\""))
                    {
                        text += word + " ";
                        continue;
                    }

                    if (word == "\"")
                    {
                        if (isString)
                        {
                            isString = false;
                            tokens.Add(
                                new Token()
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.STRING,
                                    Value = text,
                                });
                            text = "";
                        }
                        else
                        {
                            isString = true;
                            text += " ";
                        }
                    }
                    else if (word.StartsWith("\"") && word.EndsWith("\""))
                    {
                        tokens.Add(
                            new Token()
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.STRING,
                                Value = word.Substring(1, word.Length - 2),
                            });
                    }

                    else if (word.StartsWith("\""))
                    {
                        if (isString)
                        {
                            isString = false;
                            tokens.Add(
                                new Token()
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.STRING,
                                    Value = text,
                                });
                            text = "";
                        }
                        else
                        {
                            isString = true;
                            text = word.Substring(1) + " ";
                        }
                    }
                    else if (word.EndsWith("\""))
                    {
                        isString = false;
                        text += word.Substring(0, word.Length - 1);
                        tokens.Add(
                            new Token()
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.STRING,
                                Value = text,
                            });
                        text = "";
                    }
                    else switch (word)
                    {
                        case "LET":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.LET,
                                    Value = "LET"
                                });
                            break;
                        case "PRINT":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.PRINT,
                                    Value = "PRINT"
                                });
                            break;
                        case "=":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.EQUALS,
                                    Value = word
                                });
                            break;
                        case "==":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.COMPARE,
                                    Value = word
                                });
                            break;
                        case "!=":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.COMPARE,
                                    Value = word
                                });
                            break;
                        case "<":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.COMPARE,
                                    Value = word
                                });
                            break;
                        case ">":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.COMPARE,
                                    Value = word
                                });
                            break;
                        case ">=":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.COMPARE,
                                    Value = word
                                });
                            break;
                        case "<=":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.COMPARE,
                                    Value = word
                                });
                            break;
                        case "NOT":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.NOT,
                                    Value = word
                                });
                            break;
                        case "TRUE":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.BOOLEAN,
                                    Value = word
                                });
                            break;
                        case "FALSE":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.BOOLEAN,
                                    Value = word
                                });
                            break;
                        case "IF":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.IF,
                                    Value = word
                                });
                            break;
                        case "ELSE":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.ELSE,
                                    Value = word
                                });
                            break;
                        case "THEN":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.THEN,
                                    Value = word
                                });
                            break;
                        case "ENDIF":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.ENDIF,
                                    Value = word
                                });
                            break;
                        default:
                        {
                            if (Regex.IsMatch(word, "^\\d"))
                            {
                                if(Regex.IsMatch(word, @"[\+\-\*\(\)\\]"))
                                {
                                    tokens.Add(
                                        new Token
                                        {
                                            Index = tokens.Count,
                                            LineNumber = lineNumber,
                                            Type = Types.EXPRESSION,
                                            Value = word
                                        });
                                }
                                else
                                {
                                    tokens.Add(
                                        new Token
                                        {
                                            Index = tokens.Count,
                                            LineNumber = lineNumber,
                                            Type = Types.NUMBER,
                                            Value = word
                                        });
                                }
                            }
                            else if (Regex.IsMatch(word, @"[\+\-\*\(\)\\]"))
                            {
                                tokens.Add(
                                    new Token
                                    {
                                        Index = tokens.Count,
                                        LineNumber = lineNumber,
                                        Type = Types.EXPRESSION,
                                        Value = word
                                    });
                            }
                            else
                            {
                                tokens.Add(
                                    new Token
                                    {
                                        Index = tokens.Count,
                                        LineNumber = lineNumber,
                                        Type = Types.UNDEFINED,
                                        Value = word,
                                    });
                            }

                            break;
                        }
                    }
                }
                tokens.Add(new Token { Index = tokens.Count, LineNumber = lineNumber, Type = Types.NEWLINE, Value = "NEWLINE" });
                lineNumber++;
            }
            tokens.ForEach(_print);
            return tokens;
        }

        private bool? _resolveBoolean(IEnumerable<Token> tokens)
        {
            var compareType = "";
            var leftSide = new List<Token>();
            var leftValue = new Token();
            var rightSide = new List<Token>();
            var rightValue = new Token();
            var isLeft = true;
            foreach (var token in tokens)
            {
                if (token.Type == Types.COMPARE)
                {
                    isLeft = false;
                    compareType = token.Value;
                }
                else if (isLeft)
                {
                    leftSide.Add(token);
                }
                else
                {
                    rightSide.Add(token);
                }
            }

            var nextIsNot = false;
            foreach (var token in leftSide)
            {
                if (token.Type == Types.NOT)
                {
                    nextIsNot = true;
                    continue;
                }
                if (token.Type != Types.UNDEFINED) continue;
                _variables.TryGetValue(token.Value, out var value);
                if (value == null)
                {
                    return null;
                }

                token.Type = _types[token.Value] == "STRING" ? Types.STRING : (_types[token.Value] == "NUMBER" ? Types.NUMBER : Types.BOOLEAN);
                token.Value = value;
                if (!nextIsNot) continue;
                if (token.Type == Types.BOOLEAN)
                {
                    token.Value = token.Value == "TRUE" ? "FALSE" : "TRUE";
                    nextIsNot = false;
                }
                else
                {
                    return null;
                }
            }
            if (leftSide.Any(t => t.Type == Types.UNDEFINED || nextIsNot))
            {
                return null;
            }
            if (leftSide.Count(t => t.Type == Types.BOOLEAN) == 1)
            {
                leftValue = new Token
                {
                    Index = 0,
                    LineNumber = 0,
                    Type = Types.BOOLEAN,
                    Value = leftSide.ElementAt(0).Value,
                };
            }
            else if (leftSide.Count(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER) == leftSide.Count)
            {
                var res = _evaluateExpression(leftSide
                    .Where(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER).Select(t => t.Value)
                    .Join(""));
                leftValue = new Token
                {
                    Index = 0,
                    LineNumber = 0,
                    Type = Types.NUMBER,
                    Value = res.ToString(),
                };
            }
            else if (leftSide.Count(t => t.Type == Types.STRING) + leftSide.Count(t => t.Type == Types.BOOLEAN) + leftSide.Count(t => t.Type == Types.NUMBER) + leftSide.Count(t => t.Type == Types.EXPRESSION) == leftSide.Count)
            {
                if (leftSide.Where(t => t.Type == Types.EXPRESSION).All(t => t.Value == "+"))
                {
                    var text = leftSide.Where(t => t.Type == Types.NUMBER || t.Type == Types.BOOLEAN || t.Type == Types.STRING).Select(t => t.Value)
                        .Join("");
                    leftValue = new Token
                    {
                        Index = 0,
                        LineNumber = 0,
                        Type = Types.STRING,
                        Value = text,
                    };
                }
                else
                {
                    return null;
                }
            }

            
            foreach (var token in rightSide)
            {
                if (token.Type != Types.UNDEFINED) continue;
                _variables.TryGetValue(token.Value, out var value);
                if (value == null)
                {
                    return null;
                }

                token.Type = _types[token.Value] == "STRING" ? Types.STRING : (_types[token.Value] == "NUMBER" ? Types.NUMBER : Types.BOOLEAN);
                token.Value = value;
                if (!nextIsNot) continue;
                if (token.Type == Types.BOOLEAN)
                {
                    token.Value = token.Value == "TRUE" ? "FALSE" : "TRUE";
                    nextIsNot = false;
                }
                else
                {
                    return null;
                }
            }
            if (rightSide.Any(t => t.Type == Types.UNDEFINED || nextIsNot))
            {
                return null;
            }

            if (rightSide.Count(t => t.Type == Types.BOOLEAN) == 1)
            {
                rightValue = new Token
                {
                    Index = 0,
                    LineNumber = 0,
                    Type = Types.BOOLEAN,
                    Value = rightSide.ElementAt(0).Value,
                };
            }

            else if (rightSide.Count(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER) == rightSide.Count)
            {
                var res = _evaluateExpression(rightSide
                    .Where(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER).Select(t => t.Value)
                    .Join(""));
                rightValue = new Token
                {
                    Index = 0,
                    LineNumber = 0,
                    Type = Types.NUMBER,
                    Value = res.ToString(),
                };
            }
            else if (rightSide.Count(t => t.Type == Types.STRING) + rightSide.Count(t => t.Type == Types.BOOLEAN) + rightSide.Count(t => t.Type == Types.NUMBER) + rightSide.Count(t => t.Type == Types.EXPRESSION) == rightSide.Count)
            {
                if (rightSide.Where(t => t.Type == Types.EXPRESSION).All(t => t.Value == "+"))
                {
                    var text = rightSide.Where(t => t.Type == Types.NUMBER || t.Type == Types.BOOLEAN || t.Type == Types.STRING).Select(t => t.Value)
                        .Join("");
                    rightValue = new Token
                    {
                        Index = 0,
                        LineNumber = 0,
                        Type = Types.STRING,
                        Value = text,
                    };
                }
                else
                {
                    return null;
                }
            }

            if (leftValue.Type != rightValue.Type)
            {
                return false;
            }

            if (leftValue.Type == Types.STRING || leftValue.Type == Types.BOOLEAN)
            {
                switch (compareType)
                {
                    case "==":
                        return leftValue.Value == rightValue.Value;
                    case "!=":
                        return leftValue.Value != rightValue.Value;
                    default:
                        return null;
                }
            }

            switch (compareType)
            {
                case "==":
                    return leftValue.Value == rightValue.Value;
                case "<":
                    return Convert.ToInt32(leftValue.Value) < Convert.ToInt32(rightValue.Value);
                case ">":
                    return Convert.ToInt32(leftValue.Value) > Convert.ToInt32(rightValue.Value);
                case "<=":
                    return Convert.ToInt32(leftValue.Value) <= Convert.ToInt32(rightValue.Value);
                case ">=":
                    return Convert.ToInt32(leftValue.Value) >= Convert.ToInt32(rightValue.Value);
                case "!=":
                    return Convert.ToInt32(leftValue.Value) != Convert.ToInt32(rightValue.Value);
                default:
                    return null;
            }

        }

        private static void _print(Token token)
        {
            System.Diagnostics.Debug.WriteLine("Type: " + token.Type + "; Value: " + token.Value + "; Index: " + token.Index);
        }

        private static void _print(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }

        private int _evaluateExpression(string expr) => Convert.ToInt32(new DataTable().Compute(expr, null));
    }
}
