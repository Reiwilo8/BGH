using System.Text;

namespace Project.Games.Persistence
{
    public static class PersistenceValueCodec
    {
        public static string DecodePossiblyJsonString(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return raw;

            raw = raw.Trim();

            if (raw.Length < 2 || raw[0] != '"' || raw[raw.Length - 1] != '"')
                return raw;

            string inner = raw.Substring(1, raw.Length - 2);

            if (inner.IndexOf('\\') < 0)
                return inner;

            var sb = new StringBuilder(inner.Length);

            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];

                if (c != '\\' || i + 1 >= inner.Length)
                {
                    sb.Append(c);
                    continue;
                }

                char n = inner[++i];

                switch (n)
                {
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;

                    case 'u':
                        if (i + 4 < inner.Length)
                        {
                            int code = 0;
                            bool ok = true;

                            for (int k = 0; k < 4; k++)
                            {
                                char h = inner[i + 1 + k];
                                int v =
                                    (h >= '0' && h <= '9') ? (h - '0') :
                                    (h >= 'a' && h <= 'f') ? (10 + (h - 'a')) :
                                    (h >= 'A' && h <= 'F') ? (10 + (h - 'A')) :
                                    -1;

                                if (v < 0) { ok = false; break; }
                                code = (code << 4) | v;
                            }

                            if (ok)
                            {
                                sb.Append((char)code);
                                i += 4;
                            }
                            else
                            {
                                sb.Append("\\u");
                            }
                        }
                        else
                        {
                            sb.Append("\\u");
                        }
                        break;

                    default:
                        sb.Append(n);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}