namespace MarketOurs.DataAPI.Services;

public class TrieNode
{
    // 子节点：Key 为字符，Value 为对应的子节点
    public Dictionary<char, TrieNode> Children { get; set; } = new();

    // 标记当前节点是否为一个敏感词的结尾
    public bool IsEnd { get; set; }
}

public class SensitiveWordFilter
{
    private readonly TrieNode _root = new();

    public SensitiveWordFilter(IEnumerable<string> keywords)
    {
        foreach (var word in keywords)
        {
            AddWord(word);
        }
    }

    // 将敏感词插入 Trie 树
    private void AddWord(string word)
    {
        var current = _root;
        foreach (var ch in word)
        {
            if (!current.Children.TryGetValue(ch, out var value))
            {
                value = new TrieNode();
                current.Children[ch] = value;
            }

            current = value;
        }

        current.IsEnd = true;
    }

    /// <summary>
    /// 检测是否存在敏感词
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>是否存在</returns>
    public bool HasSensitiveWord(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var current = _root;
            for (var j = i; j < text.Length; j++)
            {
                if (!current.Children.TryGetValue(text[j], out var next))
                {
                    break;
                }

                current = next;
                if (current.IsEnd) return true;
            }
        }

        return false;
    }
}