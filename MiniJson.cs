using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace sdk_popup
{
    /// <summary>
    /// Minimal JSON parser (v1.5.0).
    /// Supports: object, array, string, number (as double), bool, null.
    /// Kept in-tree to avoid a System.Web.Extensions dependency.
    /// </summary>
    internal class MiniJson
    {
        private readonly string _src;
        private int _pos;

        public MiniJson(string json)
        {
            _src = json ?? "";
            _pos = 0;
        }

        /// <summary>Entry point. Returns object graph: Dictionary, List, string, double, bool, or null.</summary>
        public object ParseValue()
        {
            SkipWhitespace();
            if (_pos >= _src.Length) return null;

            char c = _src[_pos];
            if (c == '{') return ParseObject();
            if (c == '[') return ParseArray();
            if (c == '"') return ParseString();
            if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
            if (MatchLiteral("true")) return true;
            if (MatchLiteral("false")) return false;
            if (MatchLiteral("null")) return null;

            throw new FormatException("Unexpected character '" + c + "' at position " + _pos);
        }

        private Dictionary<string, object> ParseObject()
        {
            var result = new Dictionary<string, object>();
            Expect('{');
            SkipWhitespace();
            if (Peek() == '}') { _pos++; return result; }

            while (true)
            {
                SkipWhitespace();
                string key = ParseString();
                SkipWhitespace();
                Expect(':');
                object value = ParseValue();
                result[key] = value;

                SkipWhitespace();
                if (Peek() == ',') { _pos++; continue; }
                if (Peek() == '}') { _pos++; break; }
                throw new FormatException("Expected ',' or '}' at position " + _pos);
            }
            return result;
        }

        private List<object> ParseArray()
        {
            var result = new List<object>();
            Expect('[');
            SkipWhitespace();
            if (Peek() == ']') { _pos++; return result; }

            while (true)
            {
                result.Add(ParseValue());
                SkipWhitespace();
                if (Peek() == ',') { _pos++; continue; }
                if (Peek() == ']') { _pos++; break; }
                throw new FormatException("Expected ',' or ']' at position " + _pos);
            }
            return result;
        }

        private string ParseString()
        {
            Expect('"');
            var sb = new StringBuilder();
            while (_pos < _src.Length)
            {
                char c = _src[_pos++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (_pos >= _src.Length) throw new FormatException("Unterminated escape");
                    char esc = _src[_pos++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _src.Length) throw new FormatException("Bad \\u escape");
                            string hex = _src.Substring(_pos, 4);
                            _pos += 4;
                            sb.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            break;
                        default: sb.Append(esc); break;
                    }
                    continue;
                }
                sb.Append(c);
            }
            throw new FormatException("Unterminated string");
        }

        private double ParseNumber()
        {
            int start = _pos;
            if (Peek() == '-') _pos++;
            while (_pos < _src.Length)
            {
                char c = _src[_pos];
                if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                    _pos++;
                else break;
            }
            string numStr = _src.Substring(start, _pos - start);
            return double.Parse(numStr, CultureInfo.InvariantCulture);
        }

        private bool MatchLiteral(string literal)
        {
            if (_pos + literal.Length > _src.Length) return false;
            if (_src.Substring(_pos, literal.Length) != literal) return false;
            _pos += literal.Length;
            return true;
        }

        private void SkipWhitespace()
        {
            while (_pos < _src.Length && char.IsWhiteSpace(_src[_pos])) _pos++;
        }

        private char Peek()
        {
            return _pos < _src.Length ? _src[_pos] : '\0';
        }

        private void Expect(char c)
        {
            SkipWhitespace();
            if (_pos >= _src.Length || _src[_pos] != c)
                throw new FormatException("Expected '" + c + "' at position " + _pos);
            _pos++;
        }
    }
}
