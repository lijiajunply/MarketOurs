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
            if (!current.Children.ContainsKey(ch))
            {
                current.Children[ch] = new TrieNode();
            }
            current = current.Children[ch];
        }
        current.IsEnd = true;
    }

    // 仅检测是否存在敏感词（性能极高，用于快速前置审核）
    public bool HasSensitiveWord(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            TrieNode current = _root;
            for (int j = i; j < text.Length; j++)
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
