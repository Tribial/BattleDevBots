using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Xsl;
using DevBots.Data.Interfaces;
using DevBots.Services.Interfaces;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Enums;
using DevBots.Shared.Models;
//using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Internal;

namespace DevBots.Services
{
    public class LanguageService : ILanguageService
    {
        private Dictionary<string, string> _variables;
        private Dictionary<string, string> _types;
        private List<Function> _functions = new List<Function>();
        private While _while;
        private Robot forRobot;
        private const string ConsoleDebug = "DEBUG INFO: ";

        private readonly IScriptRepository _scriptRepository;

        public LanguageService(IScriptRepository scriptRepository)
        {
            _scriptRepository = scriptRepository;
        }

        public Responses<RobotCommand> Decode(long scriptId, long userId)
        {
            var result = new Responses<RobotCommand>();
            var scriptObj = _scriptRepository.Get(s => s.Id == scriptId);
            if (scriptObj.Owner.UserId != userId)
            {
                result.Errors.Add("You can't user this script since you are not the owner");
                return result;
            }

            forRobot = scriptObj.ForRobot;
            var scriptPath = scriptObj.ServerPath;
            _variables = new Dictionary<string, string>();
            _types = new Dictionary<string, string>();
            scriptPath = Path.GetFullPath(scriptPath);
            if (!File.Exists(scriptPath))
            {
                result.Errors.Add("Something went wrong. This script does not exist.");
                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                
                return result;
            }

            var script = File.ReadAllText(scriptPath);
            var tokens = Lexer(script);
            result.Model = Parser(tokens, false, new List<Token>(), new Function(), false).ToList();
            return result;
        }

        private IEnumerable<RobotCommand> Parser(IEnumerable<Token> tokens, bool isFunc, List<Token> funcParams, Function function, bool isWhile)
        {
            _print("New execution of Parser ===========================");
            _print("Tokens: ");
            foreach (var token in tokens)
            {
                _print(token);
            }
            _print("Param tokens: ");
            funcParams.ForEach(_print);
            _print("Function: ");
            _print(function.Name ?? "No name");
            function.Params.ForEach(_print);
            function.Tokens.ForEach(_print);
            _print("===============================");
            var result = new List<RobotCommand>();
            var tokenGroups = new List<List<Token>>();
            var tokenGroup = new List<Token>();
            var expectEndIf = 0;
            var expectEndWhile = 0;
            var expectedEndFunc = 0;
            foreach (var token in tokens)
            {
                if (token.Type == Types.IF)
                {
                    expectEndIf++;
                }
                else if (token.Type == Types.END_IF)
                {
                    expectEndIf--;
                    if (expectEndIf < 0)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = "An END_IF can't be before an IF"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }
                }
                else if (token.Type == Types.FUNC)
                {
                    expectedEndFunc++;
                }
                else if (token.Type == Types.END_FUNC)
                {
                    expectedEndFunc--;
                    if (expectedEndFunc < 0)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = "An END_FUNC can't be before an FUNC"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }
                }
                else if (token.Type == Types.WHILE)
                {
                    expectEndWhile++;
                }
                else if (token.Type == Types.END_WHILE)
                {
                    expectEndWhile--;
                    if (expectEndWhile < 0)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = "An END_WHILE can't be before an WHILE"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

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
                    Error = "Each IF should end with and END_IF"
                });
                return result;
            }

            if (expectedEndFunc != 0)
            {
                result.Add(new RobotCommand
                {
                    Error = "Each FUNC should end with and END_FUNC"
                });
                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                
                return result;
            }

            if (expectEndWhile != 0)
            {
                result.Add(new RobotCommand
                {
                    Error = "Each WHILE should end with and END_WHILE"
                });
                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                return result;
            }

            var isInWhile = false;
            var shouldSkipToEndWhile = false;
            var whileIteration = 0;
            var shouldDo = new List<bool> {true};
            var funcName = "";
            foreach (var tGroup in tokenGroups)
            {
                if (shouldSkipToEndWhile)
                {
                    if (tGroup.Count == 1)
                    {
                        if (tGroup.First().Type == Types.END_WHILE)
                        {
                            shouldSkipToEndWhile = false;
                        }
                    }
                    continue;
                }
                //Collecting all while values
                if (isInWhile)
                {
                    if (tGroup.Count == 1 && tGroup.First().Type == Types.END_WHILE)
                    {
                        isInWhile = false;
                    }
                    else
                    {
                        tGroup.ForEach(t => _while.Tokens.Add(new Token
                        {
                            Index = -1,
                            LineNumber = t.LineNumber,
                            Type = t.Type,
                            Value = t.Value,
                        }));
                        _while.Tokens.Add(new Token
                        {
                            Index = -1,
                            LineNumber = -1,
                            Type = Types.NEWLINE,
                            Value = "NEWLINE",
                        });
                        continue;
                    }
                }
                //executing while
                if (_while != null && !isWhile)
                {
                    while (true)
                    {
                        whileIteration++;
                        if (whileIteration > 50)
                        {
                            result.Add(new RobotCommand
                            {
                                Error = "Your while can't iterate more than 50 times",
                            });
                            return result;
                        }
                        var conditions = new List<Token>();
                        foreach (var token in _while.Condition)
                        {
                            conditions.Add(new Token
                            {
                                Index = -1,
                                LineNumber = token.LineNumber,
                                Type = token.Type,
                                Value = token.Value,
                            });
                        }
                        var whileTokens = new List<Token>();
                        _while.Tokens.ForEach(t =>
                        {
                            whileTokens.Add(new Token
                            {
                                Index = -1,
                                LineNumber = t.LineNumber,
                                Type = t.Type,
                                Value = t.Value,
                            });
                        });
                        if (conditions.Count == 1)
                        {
                            var condition = conditions.First();
                            if (condition.Type == Types.BOOLEAN)
                            {
                                if (condition.Value == "TRUE")
                                {
                                    result.AddRange(Parser(whileTokens, isFunc, funcParams, function, true).ToList());
                                }
                                else
                                {
                                    _while = null;
                                    break;
                                }
                            }
                            else if (condition.Type == Types.UNDEFINED)
                            {
                                if (isFunc)
                                {
                                    if (function.Params.Contains(condition.Value))
                                    {
                                        var paramIndex = function.Params.IndexOf(condition.Value);
                                        var paramToken = funcParams.ElementAt(paramIndex);
                                        if (paramToken.Type == Types.BOOLEAN)
                                        {
                                            if (paramToken.Value == "TRUE")
                                            {
                                                result.AddRange(Parser(whileTokens, isFunc, funcParams, function, true).ToList());
                                            }
                                            else
                                            {
                                                _while = null;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            result.Add(new RobotCommand
                                            {
                                                Error = $"At line {condition.LineNumber}. WHILE statement accepts only BOOLEAN"
                                            });
                                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                            return result;
                                        }
                                    }
                                    continue;
                                }
                                _variables.TryGetValue(condition.Value, out var value);
                                if (value == null)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"Undefined variable at line {condition.LineNumber}"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                    return result;
                                }

                                if (_types[condition.Value] == "BOOLEAN")
                                {
                                    if (value == "TRUE")
                                    {
                                        result.AddRange(Parser(whileTokens, isFunc, funcParams, function, true).ToList());
                                    }
                                    else
                                    {
                                        _while = null;
                                        break;
                                    }
                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {condition.LineNumber}. WHILE statement accepts only BOOLEAN"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                    return result;
                                }
                            }
                            else
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"At line {condition.LineNumber}. IF statement accepts only BOOLEAN"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                return result;
                            }
                        }
                        else if (conditions.Count == 2)
                        {
                            if (conditions.ElementAt(0).Type == Types.NOT)
                            {
                                if (conditions.ElementAt(1).Type == Types.BOOLEAN)
                                {
                                    if (conditions.ElementAt(1).Value != "TRUE")
                                    {
                                        result.AddRange(Parser(whileTokens, isFunc, funcParams, function, true).ToList());
                                    }
                                    else
                                    {
                                        _while = null;
                                        break;
                                    }
                                }
                                else if (conditions.ElementAt(1).Type == Types.UNDEFINED)
                                {
                                    if (isFunc)
                                    {
                                        if (function.Params.Contains(conditions.ElementAt(1).Value))
                                        {
                                            var paramIndex = function.Params.IndexOf(conditions.ElementAt(1).Value);
                                            var paramToken = funcParams.ElementAt(paramIndex);
                                            if (paramToken.Type == Types.BOOLEAN)
                                            {
                                                if (paramToken.Value != "TRUE")
                                                {
                                                    result.AddRange(Parser(whileTokens, isFunc, funcParams, function, true).ToList());
                                                }
                                                else
                                                {
                                                    _while = null;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                result.Add(new RobotCommand
                                                {
                                                    Error = $"At line {conditions.ElementAt(1).LineNumber}. WHILE statement accepts only BOOLEAN"
                                                });
                                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                                return result;
                                            }
                                            continue;
                                        }
                                    }
                                    _variables.TryGetValue(conditions.ElementAt(1).Value, out var value);
                                    if (value == null)
                                    {
                                        result.Add(new RobotCommand
                                        {
                                            Error = $"Undefined variable at line {conditions.ElementAt(1).LineNumber}"
                                        });
                                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                        return result;
                                    }

                                    if (_types[conditions.ElementAt(1).Value] == "BOOLEAN")
                                    {
                                        if (value != "TRUE")
                                        {
                                            result.AddRange(Parser(whileTokens, isFunc, funcParams, function, true).ToList());
                                        }
                                        else
                                        {
                                            _while = null;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        result.Add(new RobotCommand
                                        {
                                            Error = $"At line {conditions.ElementAt(1).LineNumber}. WHILE statement accepts only BOOLEAN"
                                        });
                                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                        return result;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (conditions.Any(c => c.Type == Types.COMPARE))
                            {
                                var res = _resolveBoolean(conditions, isFunc, function, funcParams);
                                if (res == null)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"An error occured at line {conditions.First().LineNumber}"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                    return result;
                                }

                                if (res == true)
                                {
                                    result.AddRange(Parser(whileTokens, isFunc, funcParams, function, true).ToList());
                                }
                                else
                                {
                                    _while = null;
                                    break;
                                }
                            }
                        }
                    }
                    continue;
                }
                //Set function end when found END_FUNC
                if (tGroup.Count == 1 && tGroup.ElementAt(0).Type == Types.END_FUNC)
                {
                    funcName = "";
                    continue;
                }
                if (funcName != "")
                {
                    if (tGroup.Any(t => t.Type == Types.FUNC))
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"At line {tGroup.ElementAt(0).LineNumber}, you can't declare a function inside a function"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                        return result;
                    }
                    var func = _functions.First(f => f.Name == funcName);
                    tGroup.ForEach(t => func.Tokens.Add(t));
                    func.Tokens.Add(new Token
                    {
                        Index = -1,
                        LineNumber = -1,
                        Type = Types.NEWLINE,
                        Value = "NEWLINE",
                    });
                    _functions.RemoveAll(f => f.Name == funcName);
                    _functions.Add(func);
                    //_functions.ForEach(f => f.Tokens.ForEach(_print));
                    continue;
                }
                if (tGroup.Count == 0)
                {
                    continue;
                }
                if (tGroup.ElementAt(0).Type == Types.IF)
                {
                    shouldDo.Add(shouldDo.Last());
                } 
                switch (tGroup.Count)
                {
                    case 1 when tGroup.First().Type == Types.END_IF:
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

                if (tGroup.Count >= 1 && tGroup.ElementAt(0).Type == Types.UNDEFINED)
                {
                    var func = _functions.FirstOrDefault(f => f.Name == tGroup.ElementAt(0).Value);
                    if (func == null)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"Undefined variable or function at line {tGroup.ElementAt(0).LineNumber}"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }

                    if (tGroup.Count > 1)
                    {
                        if (tGroup.ElementAt(1).Type != Types.PARAMS)
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"After a function call, the only accepted token is PARAM, at line {tGroup.ElementAt(0).LineNumber}"
                            });
                            return result;
                        }

                        if (tGroup.Count < 3)
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"After PARAM you should type parameters, at line {tGroup.ElementAt(0).LineNumber}"
                            });
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                            
                            return result;
                        }

                        var paramTokens = tGroup.GetRange(2, tGroup.Count - 2);

                        var nextIsNot = false;
                        paramTokens.ForEach(t =>
                        {
                            if (t.Type == Types.NOT)
                            {
                                nextIsNot = true;
                                return;
                            }
                            if (t.Type != Types.UNDEFINED) return;
                            if (isFunc)
                            {
                                if (function.Params.Any(p => p == t.Value))
                                {
                                    var paramIndex = function.Params.IndexOf(t.Value);
                                    var paramToken = funcParams.ElementAt(paramIndex);
                                    t.Type = paramToken.Type;
                                    t.Value = paramToken.Value;
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
                                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                        
                                        t.Type = Types.UNDEFINED;
                                    }
                                    return;
                                }
                            }
                            _variables.TryGetValue(t.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand()
                                {
                                    Error = $"An error occured in line {t.LineNumber}",
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
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
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
                                    t.Type = Types.UNDEFINED;
                                }
                            }
                        });
                        if (paramTokens.Any(t => t.Type == Types.UNDEFINED) || nextIsNot)
                        {
                            if (nextIsNot)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"At line {tGroup.ElementAt(0).LineNumber} NOT can be used on BOOLEAN only"
                                });
                            }
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                            
                            return result;
                        }

                        paramTokens.RemoveAll(p => p.Type == Types.NOT);
                        if (func.Params.Count != paramTokens.Count)
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"At line {tGroup.ElementAt(0).LineNumber}, the number of given params does not match the number of params in the declaration"
                            });
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                            
                            return result;
                        }
                        //_print("Prams number " + paramTokens.Count);
                        //paramTokens.ForEach(_print);
                        func.Tokens.ForEach(token =>
                        {

                        });
                        var funcTokens = new List<Token>();
                        func.Tokens.ForEach(t =>
                        {
                            funcTokens.Add(new Token
                            {
                                Index = t.Index,
                                LineNumber = t.LineNumber,
                                Type = t.Type,
                                Value = t.Value,
                            });
                        });
                        result.AddRange(Parser(funcTokens, true, paramTokens, func, false));
                        if (result.Any(r => r.Error != null))
                        {
                            return result;
                        }
                    }

                    
                }
               
                //FUNC DEC
                else if (tGroup.Count >= 2 && tGroup.ElementAt(0).Type == Types.FUNC && 
                    tGroup.ElementAt(1).Type == Types.UNDEFINED)
                {
                    if (funcName != "")
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"At line {tGroup.ElementAt(0).LineNumber}, you can't declare a function inside a function"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }

                    if (shouldDo.Count > 1)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"At line {tGroup.ElementAt(0).LineNumber}, you can't declare a function inside an IF"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }

                    if (_functions.Any(f => f.Name == tGroup.ElementAt(1).Value))
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"At line {tGroup.ElementAt(0).LineNumber}, function called {tGroup.ElementAt(1).Value} already exists"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }

                    var paramExists = false;
                    _functions.ForEach(f => f.Params.ForEach(p =>
                    {
                        if (p == tGroup.ElementAt(1).Value)
                        {
                            paramExists = true;
                        }
                    }));
                    if (paramExists)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"At line {tGroup.ElementAt(0).LineNumber}, parameter called {tGroup.ElementAt(1).Value} already exists"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }
                    _variables.TryGetValue(tGroup.ElementAt(1).Value, out var variable);
                    if (variable != null)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"At line {tGroup.ElementAt(0).LineNumber}, variable called {tGroup.ElementAt(1).Value} already exists"
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                        return result;
                    }

                    var newFunc = new Function();
                    newFunc.Name = tGroup.ElementAt(1).Value;
                    funcName = newFunc.Name;
                    if (tGroup.Count > 2)
                    {
                        if (tGroup.ElementAt(2).Type == Types.PARAMS)
                        {
                            var parameters = tGroup.GetRange(3, tGroup.Count - 3);
                            if (parameters.Count == 0)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"At line {tGroup.ElementAt(0).LineNumber}, after PARAMS at least one parameters needs to be added"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                                return result;
                            }

                            parameters.ForEach(p =>
                            {
                                paramExists = false;
                                _functions.ForEach(f => f.Params.ForEach(p2 =>
                                {
                                    if (p2 == p.Value)
                                    {
                                        paramExists = true;
                                    }
                                }));
                                if (paramExists)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {tGroup.ElementAt(0).LineNumber}, parameter called {p.Value} already exists"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
                                    return;
                                }
                                _variables.TryGetValue(p.Value, out var paramVar);
                                if (paramVar != null)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {tGroup.ElementAt(0).LineNumber}, variable called {p.Value} already exists"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
                                }
                                else
                                {
                                    newFunc.Params.Add(p.Value);
                                }
                            });
                            if (result.Any(r => r.Error != null || r.Error != ""))
                            {
                                return result;
                            }
                        }
                        else
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"At line {tGroup.ElementAt(0).LineNumber}, after FUNC name, are expected PARAMS"
                            });
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                            
                            return result;
                        }
                        _functions.Add(newFunc);
                    }
                }
                //WHILE---------------------------------------------------------------------
                else if (tGroup.ElementAt(0).Type == Types.WHILE && tGroup.Last().Type == Types.THEN)
                {
                    if (isInWhile)
                    {
                        result.Add(new RobotCommand
                        {
                            Error = $"At line {tGroup.ElementAt(0).LineNumber}, you can't have a while inside a while"
                        });
                        return result;
                    }

                    var whileCondition = new List<Token>();
                    foreach (var token in tGroup.GetRange(1, tGroup.Count - 2))
                    {
                        whileCondition.Add(new Token
                        {
                            Index = -1,
                            LineNumber = token.LineNumber,
                            Type = token.Type,
                            Value = token.Value,
                        });
                    }
                    var conditions = tGroup.GetRange(1, tGroup.Count - 2);
                    if (conditions.Count == 1)
                    {
                        var condition = conditions.First();
                        if (condition.Type == Types.BOOLEAN)
                        {
                            isInWhile = condition.Value == "TRUE";
                            shouldSkipToEndWhile = !isInWhile;
                            if (isInWhile)
                            {
                                _while = new While { Condition = whileCondition };
                            }
                            _print("Do while " + isInWhile.ToString());
                        }
                        else if (condition.Type == Types.UNDEFINED)
                        {
                            if (isFunc)
                            {
                                if (function.Params.Contains(condition.Value))
                                {
                                    var paramIndex = function.Params.IndexOf(condition.Value);
                                    var paramToken = funcParams.ElementAt(paramIndex);
                                    if (paramToken.Type == Types.BOOLEAN)
                                    {
                                        isInWhile = paramToken.Value == "TRUE";
                                        shouldSkipToEndWhile = !isInWhile;
                                        if (isInWhile)
                                        {
                                            _while = new While {Condition = whileCondition };
                                        }
                                        _print("Do while " + isInWhile.ToString());
                                    }
                                    else
                                    {
                                        result.Add(new RobotCommand
                                        {
                                            Error = $"At line {condition.LineNumber}. WHILE statement accepts only BOOLEAN"
                                        });
                                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                        return result;
                                    }
                                }
                                continue;
                            }
                            _variables.TryGetValue(condition.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"Undefined variable at line {condition.LineNumber}"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                return result;
                            }

                            if (_types[condition.Value] == "BOOLEAN")
                            {
                                isInWhile = value == "TRUE";
                                shouldSkipToEndWhile = !isInWhile;
                                if (isInWhile)
                                {
                                    _while = new While { Condition = whileCondition };
                                }
                                _print("Do while " + isInWhile.ToString());
                            }
                            else
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"At line {condition.LineNumber}. WHILE statement accepts only BOOLEAN"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                return result;
                            }
                        }
                        else
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"At line {condition.LineNumber}. WHILE statement accepts only BOOLEAN"
                            });
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                            return result;
                        }
                    }
                    else if (conditions.Count == 2)
                    {
                        if (conditions.ElementAt(0).Type == Types.NOT)
                        {
                            if (conditions.ElementAt(1).Type == Types.BOOLEAN)
                            {
                                isInWhile = conditions.ElementAt(1).Value != "TRUE";
                                shouldSkipToEndWhile = !isInWhile;
                                if (isInWhile)
                                {
                                    _while = new While { Condition = whileCondition };
                                }
                                _print("Do while " + isInWhile.ToString());
                            }
                            else if (conditions.ElementAt(1).Type == Types.UNDEFINED)
                            {
                                if (isFunc)
                                {
                                    if (function.Params.Contains(conditions.ElementAt(1).Value))
                                    {
                                        var paramIndex = function.Params.IndexOf(conditions.ElementAt(1).Value);
                                        var paramToken = funcParams.ElementAt(paramIndex);
                                        if (paramToken.Type == Types.BOOLEAN)
                                        {
                                            isInWhile = paramToken.Value != "TRUE";
                                            shouldSkipToEndWhile = !isInWhile;
                                            if (isInWhile)
                                            {
                                                _while = new While { Condition = whileCondition };
                                            }
                                            _print("Do while " + isInWhile.ToString());
                                        }
                                        else
                                        {
                                            result.Add(new RobotCommand
                                            {
                                                Error = $"At line {conditions.ElementAt(1).LineNumber}. WHILE statement accepts only BOOLEAN"
                                            });
                                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                            return result;
                                        }
                                        continue;
                                    }
                                }
                                _variables.TryGetValue(conditions.ElementAt(1).Value, out var value);
                                if (value == null)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"Undefined variable at line {conditions.ElementAt(1).LineNumber}"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                    return result;
                                }

                                if (_types[conditions.ElementAt(1).Value] == "BOOLEAN")
                                {
                                    isInWhile = value != "TRUE";
                                    shouldSkipToEndWhile = !isInWhile;
                                    if (isInWhile)
                                    {
                                        _while = new While { Condition = whileCondition };
                                    }
                                    _print("Do while " + isInWhile.ToString());
                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {conditions.ElementAt(1).LineNumber}. WHILE statement accepts only BOOLEAN"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                    return result;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (conditions.Any(c => c.Type == Types.COMPARE))
                        {
                            var res = _resolveBoolean(conditions, isFunc, function, funcParams);
                            if (res == null)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"An error occured at line {conditions.First().LineNumber}"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());

                                return result;
                            }
                            isInWhile = res == true;
                            shouldSkipToEndWhile = !isInWhile;
                            if (isInWhile)
                            {
                                _while = new While { Condition = whileCondition };
                            }
                            _print("Do while " + isInWhile.ToString());
                        }
                    }
                }
                //IF---------------------------------------------------------------------
                else if (tGroup.ElementAt(0).Type == Types.IF && tGroup.Last().Type == Types.THEN)
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
                            if (isFunc)
                            {
                                if (function.Params.Contains(condition.Value))
                                {
                                    var paramIndex = function.Params.IndexOf(condition.Value);
                                    var paramToken = funcParams.ElementAt(paramIndex);
                                    if (paramToken.Type == Types.BOOLEAN)
                                    {
                                        shouldDo.RemoveAt(shouldDo.Count - 1);
                                        shouldDo.Add(paramToken.Value == "TRUE");
                                    }
                                    else
                                    {
                                        result.Add(new RobotCommand
                                        {
                                            Error = $"At line {condition.LineNumber}. IF statement accepts only BOOLEAN"
                                        });
                                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                        
                                        return result;
                                    }
                                }
                                continue;
                            }
                            _variables.TryGetValue(condition.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"Undefined variable at line {condition.LineNumber}"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
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
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                                return result;
                            }
                        }
                        else
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"At line {condition.LineNumber}. IF statement accepts only BOOLEAN"
                            });
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                            
                            return result;
                        }
                    }
                    else if (conditions.Count == 2)
                    {
                        if (conditions.ElementAt(0).Type == Types.NOT)
                        {
                            if (conditions.ElementAt(1).Type == Types.BOOLEAN)
                            {
                                shouldDo.RemoveAt(shouldDo.Count - 1);
                                shouldDo.Add(conditions.ElementAt(1).Value != "TRUE");
                            }
                            else if (conditions.ElementAt(1).Type == Types.UNDEFINED)
                            {
                                if (isFunc)
                                {
                                    if (function.Params.Contains(conditions.ElementAt(1).Value))
                                    {
                                        var paramIndex = function.Params.IndexOf(conditions.ElementAt(1).Value);
                                        var paramToken = funcParams.ElementAt(paramIndex);
                                        if (paramToken.Type == Types.BOOLEAN)
                                        {
                                            shouldDo.RemoveAt(shouldDo.Count - 1);
                                            shouldDo.Add(paramToken.Value != "TRUE");
                                        }
                                        else
                                        {
                                            result.Add(new RobotCommand
                                            {
                                                Error = $"At line {conditions.ElementAt(1).LineNumber}. IF statement accepts only BOOLEAN"
                                            });
                                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                            
                                            return result;
                                        }
                                        continue;
                                    }
                                }
                                _variables.TryGetValue(conditions.ElementAt(1).Value, out var value);
                                if (value == null)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"Undefined variable at line {conditions.ElementAt(1).LineNumber}"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
                                    return result;
                                }

                                if (_types[conditions.ElementAt(1).Value] == "BOOLEAN")
                                {
                                    shouldDo.RemoveAt(shouldDo.Count - 1);
                                    shouldDo.Add(value != "TRUE");
                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {conditions.ElementAt(1).LineNumber}. IF statement accepts only BOOLEAN"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
                                    return result;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (conditions.Any(c => c.Type == Types.COMPARE))
                        {
                            var res = _resolveBoolean(conditions, isFunc, function, funcParams);
                            if (res == null)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"An error occured at line {conditions.First().LineNumber}"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                                return result;
                            }
                            shouldDo.RemoveAt(shouldDo.Count - 1);
                            shouldDo.Add(res == true);
                        }
                    }
                }
                //PRINT
                else if (tGroup.ElementAt(0).Type == Types.PRINT)
                {
                    if (groupLength == 2)
                    {
                        if (tGroup.ElementAt(1).Type == Types.UNDEFINED)
                        {
                            if (isFunc)
                            {
                                if (function.Params.Contains(tGroup.ElementAt(1).Value))
                                {
                                    var paramIndex = function.Params.IndexOf(tGroup.ElementAt(1).Value);
                                    var paramToken = funcParams.ElementAt(paramIndex);
                                    tGroup.ElementAt(1).Type = paramToken.Type;
                                    tGroup.ElementAt(1).Value = paramToken.Value;
                                }
                            }
                        }

                        switch (tGroup.ElementAt(1).Type)
                        {
                            case Types.EXPRESSION:
                                try
                                {
                                    var res = _evaluateExpression(tGroup.ElementAt(1).Value).ToString();
                                    result.Add(new RobotCommand
                                    {
                                        Console = _evaluateExpression(tGroup.ElementAt(1).Value).ToString(),
                                    });
                                }
                                catch (Exception)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"An error occured while processing the expression on line {tGroup.ElementAt(1).LineNumber}",
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
                                }

                                break;
                            case Types.BOOLEAN:
                            case Types.NUMBER:
                            case Types.STRING:
                                result.Add(new RobotCommand
                                {
                                    Console = tGroup.ElementAt(1).Value
                                });
                                break;
                            case Types.UNDEFINED:
                            {
                                if (isFunc)
                                {
                                    if (function.Params.Contains(tGroup.ElementAt(1).Value))
                                    {
                                        var paramIndex = function.Params.IndexOf(tGroup.ElementAt(1).Value);
                                        var paramToken = funcParams.ElementAt(paramIndex);
                                        result.Add(new RobotCommand
                                        {
                                            Console = paramToken.Value,
                                        });
                                        continue;
                                    }
                                }
                                _variables.TryGetValue(tGroup.ElementAt(1).Value, out var value);
                                if (value == null)
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"Variable {tGroup.ElementAt(1).Value} is unassigned"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
                                    return result;
                                }

                                result.Add(new RobotCommand
                                {
                                    Console = value,
                                });

                                break;
                            }
                            default:
                                result.Add(new RobotCommand
                                {
                                    Error = $"Type {tGroup.ElementAt(1).Type.ToString()} is not a valid type at line {tGroup.ElementAt(0).LineNumber}",
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                                return result;
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
                            if (isFunc)
                            {
                                if (function.Params.Contains(t.Value))
                                {
                                    var paramIndex = function.Params.IndexOf(t.Value);
                                    var paramToken = funcParams.ElementAt(paramIndex);
                                    t.Type = paramToken.Type;
                                    t.Value = paramToken.Value;
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
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    

                                    return;
                                }
                            }
                            _variables.TryGetValue(t.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand()
                                {
                                    Error = $"An error occured in line {t.LineNumber}, unknown value {t.Value}",
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
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
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
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
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                            
                            return result;
                        }
                        else if (tGroup.Count(t => t.Type == Types.COMPARE) == 1)
                        {
                            var boolean = _resolveBoolean(tGroup.GetRange(1, tGroup.Count - 1), isFunc, function, funcParams);
                            switch (boolean)
                            {
                                case null:
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"An error occured in line {tGroup.ElementAt(0).LineNumber}"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
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
                            try
                            {
                                var res = _evaluateExpression(tGroup
                                    .Where(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER)
                                    .Select(t => t.Value)
                                    .Join(""));
                                result.Add(new RobotCommand()
                                {
                                    Console = res.ToString(),
                                });
                            }
                            catch (Exception)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"An error occured while processing the expression on line {tGroup.ElementAt(0).LineNumber}",
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                            }
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
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                                return result;
                            }
                        }
                        else
                        {
                            result.Add(new RobotCommand
                            {
                                Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}"
                            });
                            _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                            
                            return result;
                        }
                    }
                }
                //LET
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
                            if (t.Type == Types.ROBOT)
                            {
                                if (t.Value == "GET_POS_X")
                                {

                                }
                                else if (t.Value == "GET_POS_Y")
                                {

                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"At line {t.LineNumber}, you can't use this robot command here"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                }

                                return;
                            }
                            if (t.Type != Types.UNDEFINED) return;
                            if (isFunc)
                            {
                                if (function.Params.Contains(t.Value))
                                {
                                    var paramIndex = function.Params.IndexOf(t.Value);
                                    var paramToken = funcParams.ElementAt(paramIndex);
                                    t.Type = paramToken.Type;
                                    t.Value = paramToken.Value;
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
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    

                                    return;
                                }
                            }
                            _variables.TryGetValue(t.Value, out var value);
                            if (value == null)
                            {
                                result.Add(new RobotCommand()
                                {
                                    Error = $"An error occured in line {t.LineNumber}",
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
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
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
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
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
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
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                            }
                            else
                            {
                                _variables[tGroup.ElementAt(1).Value] = tGroup.FirstOrDefault(t => t.Type == Types.BOOLEAN)?.Value;
                                _types[tGroup.ElementAt(1).Value] = "BOOLEAN";
                            }
                        }
                        else if (tGroup.Count(t => t.Type == Types.COMPARE) == 1)
                        {
                            var boolean = _resolveBoolean(tGroup.GetRange(3, tGroup.Count - 3), isFunc, function, funcParams);
                            switch (boolean)
                            {
                                case null:
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"An error occured in line {tGroup.ElementAt(0).LineNumber}"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    
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
                            try
                            {
                                var res = _evaluateExpression(tGroup
                                    .Where(t => t.Type == Types.EXPRESSION || t.Type == Types.NUMBER)
                                    .Select(t => t.Value)
                                    .Join(""));
                                _variables[tGroup.ElementAt(1).Value] = res.ToString();
                                _types[tGroup.ElementAt(1).Value] = "NUMBER";

                            }
                            catch (Exception)
                            {
                                result.Add(new RobotCommand
                                {
                                    Error = $"An error occured while processing the expression on line {tGroup.ElementAt(1).LineNumber}",
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                            }
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
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                
                            }
                        }
                    }
                    else
                    {
                        result.Add(new RobotCommand()
                        {
                            Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}, after LET expected 'variableName = value'",
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                        
                    }
                }
                //ROBOT
                else if (tGroup.Count >= 1 && tGroup.ElementAt(0).Type == Types.ROBOT)
                {
                    if (tGroup.Count == 1)
                    {
                        var token = new RobotCommand();
                        var tIndex = 0;
                        switch (tGroup.ElementAt(0).Value)
                        {
                            case "MOVE_FORWARD":
                                if (result.Count != 0)
                                {
                                    token = result.LastOrDefault(t => t.CountsAsCommand);
                                    if (token == null)
                                    {
                                        tIndex = 0;
                                        result[tIndex].Type = "MOVE";
                                        result[tIndex].Direction = 1;
                                        result[tIndex].CountsAsCommand = true;
                                    }
                                    else
                                    {
                                        tIndex = result.LastIndexOf(token);
                                        if (tIndex == result.Count - 1)
                                        {
                                            token = new RobotCommand
                                            {
                                                Type = "MOVE", Direction = 1, CountsAsCommand = true
                                            };
                                            result.Add(token);
                                        }
                                        else
                                        {
                                            tIndex++;
                                            result[tIndex].Type = "MOVE";
                                            result[tIndex].Direction = 1;
                                            result[tIndex].CountsAsCommand = true;
                                        }
                                    }
                                }
                                else
                                {
                                    token.Type = "MOVE";
                                    token.Direction = 1;
                                    token.CountsAsCommand = true;
                                    result.Add(token);
                                }
                                break;
                            case "MOVE_RIGHT":
                                if (result.Count != 0)
                                {
                                    token = result.LastOrDefault(t => t.CountsAsCommand);
                                    if (token == null)
                                    {
                                        tIndex = 0;
                                        result[tIndex].Type = "MOVE";
                                        result[tIndex].Direction = 2;
                                        result[tIndex].CountsAsCommand = true;
                                    }
                                    else
                                    {
                                        tIndex = result.LastIndexOf(token);
                                        if (tIndex == result.Count - 1)
                                        {
                                            token = new RobotCommand
                                            {
                                                Type = "MOVE", Direction = 2, CountsAsCommand = true
                                            };
                                            result.Add(token);
                                        }
                                        else
                                        {
                                            tIndex++;
                                            result[tIndex].Type = "MOVE";
                                            result[tIndex].Direction = 2;
                                            result[tIndex].CountsAsCommand = true;
                                        }
                                    }
                                }
                                else
                                {
                                    token.Type = "MOVE";
                                    token.Direction = 2;
                                    token.CountsAsCommand = true;
                                    result.Add(token);
                                }
                                break;
                            case "MOVE_BACK":
                                if (result.Count != 0)
                                {
                                    token = result.LastOrDefault(t => t.CountsAsCommand);
                                    if (token == null)
                                    {
                                        tIndex = 0;
                                        result[tIndex].Type = "MOVE";
                                        result[tIndex].Direction = 3;
                                        result[tIndex].CountsAsCommand = true;
                                    }
                                    else
                                    {
                                        tIndex = result.LastIndexOf(token);
                                        if (tIndex == result.Count - 1)
                                        {
                                            token = new RobotCommand
                                            {
                                                Type = "MOVE", Direction = 3, CountsAsCommand = true
                                            };
                                            result.Add(token);
                                        }
                                        else
                                        {
                                            tIndex++;
                                            result[tIndex].Type = "MOVE";
                                            result[tIndex].Direction = 3;
                                            result[tIndex].CountsAsCommand = true;
                                        }
                                    }
                                }
                                else
                                {
                                    token.Type = "MOVE";
                                    token.Direction = 3;
                                    token.CountsAsCommand = true;
                                    result.Add(token);
                                }
                                break;
                            case "MOVE_LEFT":
                                if (result.Count != 0)
                                {
                                    token = result.LastOrDefault(t => t.CountsAsCommand);
                                    if (token == null)
                                    {
                                        tIndex = 0;
                                        result[tIndex].Type = "MOVE";
                                        result[tIndex].Direction = 4;
                                        result[tIndex].CountsAsCommand = true;
                                    }
                                    else
                                    {
                                        tIndex = result.LastIndexOf(token);
                                        if (tIndex == result.Count - 1)
                                        {
                                            token = new RobotCommand
                                            {
                                                Type = "MOVE", Direction = 4, CountsAsCommand = true
                                            };
                                            result.Add(token);
                                        }
                                        else
                                        {
                                            tIndex++;
                                            result[tIndex].Type = "MOVE";
                                            result[tIndex].Direction = 4;
                                            result[tIndex].CountsAsCommand = true;
                                        }
                                    }
                                }
                                else
                                {
                                    token.Type = "MOVE";
                                    token.Direction = 4;
                                    token.CountsAsCommand = true;
                                    result.Add(token);
                                }
                                break;
                            case "ATTACK":
                                if (result.Count != 0)
                                {
                                    token = result.LastOrDefault(t => t.CountsAsCommand);
                                    if (token == null)
                                    {
                                        tIndex = 0;
                                        result[tIndex].Type = "ATTACK";
                                        result[tIndex].CountsAsCommand = true;
                                    }
                                    else
                                    {
                                        tIndex = result.LastIndexOf(token);
                                        if (tIndex == result.Count - 1)
                                        {
                                            token = new RobotCommand
                                            {
                                                Type = "ATTACK",
                                                CountsAsCommand = true
                                            };
                                            result.Add(token);
                                        }
                                        else
                                        {
                                            tIndex++;
                                            result[tIndex].Type = "ATTACK";
                                            result[tIndex].CountsAsCommand = true;
                                        }
                                    }
                                }
                                else
                                {
                                    token.Type = "ATTACK";
                                    token.CountsAsCommand = true;
                                    result.Add(token);
                                }
                                break;
                            default:
                                result.Add(new RobotCommand
                                {
                                    Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}, robot command not known"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                return result;
                        }
                    }
                    else if (tGroup.Count == 2)
                    {
                        var token = new RobotCommand();
                        var tIndex = 0;
                        switch (tGroup.ElementAt(0).Value)
                        {
                            case "TURN":
                                if (tGroup.ElementAt(1).Type == Types.NUMBER)
                                {
                                    var turnValue = Convert.ToInt32(tGroup.ElementAt(1).Value);
                                    if (turnValue != 1 && turnValue != 2 && turnValue != 3 && turnValue != 4)
                                    {
                                        result.Add(new RobotCommand
                                        {
                                            Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}, after TURN expected direction as 1, 2, 3 or 4"
                                        });
                                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                        return result;
                                    }
                                    if (result.Count != 0)
                                    {
                                        token = result.LastOrDefault(t => t.CountsAsCommand);
                                        if (token == null)
                                        {
                                            tIndex = 0;
                                            result[tIndex].Type = "TURN";
                                            result[tIndex].Direction = turnValue;
                                            result[tIndex].CountsAsCommand = true;
                                        }
                                        else
                                        {
                                            tIndex = result.LastIndexOf(token);
                                            if (tIndex == result.Count - 1)
                                            {
                                                token = new RobotCommand
                                                {
                                                    Type = "TURN",
                                                    Direction = turnValue,
                                                    CountsAsCommand = true
                                                };
                                                result.Add(token);
                                            }
                                            else
                                            {
                                                tIndex++;
                                                result[tIndex].Type = "TURN";
                                                result[tIndex].Direction = turnValue;
                                                result[tIndex].CountsAsCommand = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        token.Type = "TURN";
                                        token.Direction = turnValue;
                                        token.CountsAsCommand = true;
                                        result.Add(token);
                                    }
                                }
                                else
                                {
                                    result.Add(new RobotCommand
                                    {
                                        Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}, after TURN command is expected number"
                                    });
                                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                    return result;
                                }
                                break;
                            default:
                                result.Add(new RobotCommand
                                {
                                    Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}, robot command not known"
                                });
                                _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                                return result;
                        }
                    }
                    else
                    {
                        result.Add(new RobotCommand()
                        {
                            Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}, ROBOT command can have now only 1 word",
                        });
                        _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                    }
                }
                //ELSE
                else
                {
                    result.Add(new RobotCommand
                    {
                        Error = $"Syntax error at line {tGroup.ElementAt(0).LineNumber}"
                    });
                    _WriteLineWithError((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber());
                    
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
                        case "FUNC":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.FUNC,
                                    Value = "FUNC"
                                });
                            break;
                        case "END_FUNC":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.END_FUNC,
                                    Value = "END_FUNC"
                                });
                            break;
                        case "PARAMS":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.PARAMS,
                                    Value = "PARAMS"
                                });
                            break;
                        case "WHILE":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.WHILE,
                                    Value = "WHILE"
                                });
                            break;
                        case "END_WHILE":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.END_WHILE,
                                    Value = "END_WHILE"
                                });
                            break;
                        case "MOVE_FORWARD":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "MOVE_FORWARD"
                            });
                            break;
                        case "MOVE_RIGHT":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "MOVE_RIGHT"
                            });
                            break;
                        case "MOVE_LEFT":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "MOVE_LEFT"
                            });
                            break;
                        case "MOVE_BACK":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "MOVE_BACK"
                            });
                            break;
                        case "ATTACK":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "ATTACK"
                            });
                            break;
                        case "TURN":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "TURN"
                            });
                            break;
                        case "MY_POS_X":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "GET_POS_X"
                            });
                                break;
                        case "MY_POX_Y":
                            tokens.Add(new Token
                            {
                                Index = tokens.Count,
                                LineNumber = lineNumber,
                                Type = Types.ROBOT,
                                Value = "GET_POS_Y"
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
                        case "END_IF":
                            tokens.Add(
                                new Token
                                {
                                    Index = tokens.Count,
                                    LineNumber = lineNumber,
                                    Type = Types.END_IF,
                                    Value = word
                                });
                            break;
                        default:
                        {
                            if (Regex.IsMatch(word, "^\\d"))
                            {
                                if(Regex.IsMatch(word, @"[\+\-\*\(\)\/]"))
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
                            else if (Regex.IsMatch(word, @"[\+\-\*\(\)\/]"))
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

        private bool? _resolveBoolean(IEnumerable<Token> tokens, bool isFunc, Function function, List<Token> funcParams)
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
                if (isFunc)
                {
                    if (function.Params.Contains(token.Value))
                    {
                        var paramIndex = function.Params.IndexOf(token.Value);
                        var paramToken = funcParams.ElementAt(paramIndex);
                        token.Type = paramToken.Type;
                        token.Value = paramToken.Value;
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
                        continue;
                    }
                }
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
                try
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
                catch (Exception)
                {
                    return null;
                }
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
                if (isFunc)
                {
                    if (function.Params.Contains(token.Value))
                    {
                        var paramIndex = function.Params.IndexOf(token.Value);
                        var paramToken = funcParams.ElementAt(paramIndex);

                        token.Type = paramToken.Type;
                        token.Value = paramToken.Value;
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
                        continue;
                    }
                }
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
                try
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
                catch (Exception)
                {
                    return null;
                }
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

        private void _WriteLineWithError(int lineNumber)
        {
            _print($"<<<<<<<<<<Exception At line {lineNumber}>>>>>>>>>>");

        }
    }
}
