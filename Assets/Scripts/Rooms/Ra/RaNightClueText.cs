using System.Collections.Generic;
using System.Text;

// 밤 비석 글 (손상된 상형문자를 렌즈로 해독한 결과)
// - 밤에는 비석이 많이 깨져 있어서 해독해도 몇 글자는 "?"로 남음 (풀이에 꼭 필요한 낱말은 남겨 둠)
// - 비석 아이디(RA_P1 등)에 없는 것은 원래 글 그대로
public static class RaNightClueText
{
    private static readonly Dictionary<string, string> Damaged = new Dictionary<string, string>
    {
        { "RA_P1", "밤의 그?자는 해를 등지지 않는다. 그리운 ?으로 눕는다." },
        { "RA_P2", "회?는 이미 밤빛으로 물들었다. 빛은 더 이? 길을 찾지 않는다." },
        { "RA_P3", "달이 해를 삼키?, 시?은 거꾸로 흐른다." },
        { "RA_P4", "죽은 ?의 하루는 해가 눕는 곳에서 시?된다." },
        { "RA_P5", "밤의 ?정은 해가 눕는 곳에서 시작해 깨어나는 곳에서 끝난다. 그 사?, 그는 걷지 않?다." }
    };

    public static string Get(string clueId, string original)
    {
        return clueId != null && Damaged.TryGetValue(clueId, out string text) ? text : original;
    }

    // 상형문자 판에 넣을 줄 나누기 (한 줄 최대 maxChars 글자, 띄어쓰기 기준)
    public static string[] ToLines(string text, int maxChars)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (string word in (text ?? "").Split(' '))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > maxChars)
            {
                lines.Add(line.ToString());
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) lines.Add(line.ToString());
        return lines.ToArray();
    }
}
