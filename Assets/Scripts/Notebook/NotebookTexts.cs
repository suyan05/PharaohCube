using System.Collections.Generic;
using System.Text;

// 고고학 수첩 글 (챕터별 연구원 메모 3줄, 메모 탭 목록)
public static class NotebookTexts
{
    private static readonly Dictionary<string, string> Memos = new Dictionary<string, string>
    {
        { "프롤로그", "기자 고원 발굴 현장. 오늘도 모래뿐이었다.\n무전을 받고 간 구역에서 스핑크스의 꿈을 꾸었다.\n꿈속의 비석은 렌즈로 비춰야 읽혔다." },
        { "라", "태양의 신 라의 신전. 횃불 없이는 한 걸음도 못 간다.\n낮의 하루를 걸으면 밤의 시련이 열린다.\n해가 진 뒤에도 꺼지지 않는 불이 있다고 한다." },
        { "바스테트", "고양이 여신의 방.\n향과 기름 그릇을 지키는 여신이라고 한다." },
        { "토트", "지혜와 기록의 신 토트의 방.\n서기관의 손으로 글을 다시 써야 한다." },
        { "호루스", "매의 신 호루스의 방.\n밤하늘의 별을 이어야 한다." },
        { "소베크", "악어 신 소베크의 방.\n물이 빠지면 무언가 드러난다." },
        { "아누비스", "저승의 신 아누비스.\n저울 앞에서 마음의 무게를 단다." },
        { "특별", "발굴단 사이에 전해지는 이야기들.\n찾아낸 사람만 이 칸을 채울 수 있다." }
    };

    public static string Memo(string chapter)
    {
        return Memos.TryGetValue(chapter, out string text) ? text : "";
    }

    // 메모 탭: [노트 1] 글 ... (못 찾은 번호는 "아직 찾지 못한 메모")
    public static string NotesText()
    {
        if (NotebookNotes.Count == 0) return "아직 적어 둔 메모가 없다.\n\n신전의 비석과 단서를 조사하면 여기에 기록된다.";

        var sb = new StringBuilder();
        foreach (KeyValuePair<int, string> n in NotebookNotes.All)
        {
            sb.Append("<b>[노트 ").Append(n.Key).Append("]</b>  ").Append(n.Value).Append("\n\n");
        }
        return sb.ToString();
    }
}
