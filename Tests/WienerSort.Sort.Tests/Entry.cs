using System.Text;

namespace WienerSort.Sort.Tests;

[TestFixture]
public class EntryTests
{
    [Test]
    [TestCase("123. Hello World", 123u, "Hello World")]
    [TestCase(
        $"4294967295. It should support quite long phrases but not too long, 256 is optimal. It should support quite long phrases but not too long, 256 is optimal. It should support quite long phrases but not too long, 256 is optimal. 111111211312312321123dsfsdfdsfds66666776655",
        4294967295u,
        "It should support quite long phrases but not too long, 256 is optimal. It should support quite long phrases but not too long, 256 is optimal. It should support quite long phrases but not too long, 256 is optimal. 111111211312312321123dsfsdfdsfds66666776655")]
    public void FromLine_ParsesNumberAndPhrase(string input, uint expectedNumber, string expectedPhrase)
    {
        var inputArr = Encoding.ASCII.GetBytes(input);

        var entry = Entry.FromSpan(inputArr);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Number, Is.EqualTo(expectedNumber));
            Assert.That(entry.ToString(), Is.EqualTo(input));
            Assert.That(entry.PhraseLength, Is.EqualTo(expectedPhrase.Length));
        });
    }

    [TestCase(
        $"1. Too large case and it will fail. It should support quite long phrases but not too long, 256 is optimal. It should support quite long phrases but not too long, 256 is optimal. It should support quite long phrases but not too long, 256 is optimal. 111111211312312321123dsfsdfdsfds66666776655")]
    // TODO add validation?
    // [TestCase("429496729885u. Too large number and should fail too")]
    // [TestCase("There is no number")]
    // [TestCase("There is no number. But there is sentence")]
    // [TestCase("9There is no number")]
    public void FromLine_ThrowsOnInvalidInput(string input)
    {
        var inputArr = Encoding.ASCII.GetBytes(input);

        Assert.That(() => Entry.FromSpan(inputArr), Throws.Exception);
    }
}

[TestFixture]
public class EntryComparerTests
{
    private EntryComparer _comparer;

    [SetUp]
    public void Setup()
    {
        _comparer = new();
    }

    private static Entry MakeEntry(int number, string phrase)
    {
        var line = Encoding.ASCII.GetBytes($"{number}. {phrase}");
        return Entry.FromSpan(line);
    }

    [TestCase(1, 2, -1)]
    [TestCase(12, 2, 1)]
    [TestCase(12, 12, 0)]
    public void Compare_SamePhraseDifferentNumbers(int first, int second, int expected)
    {
        var e1 = MakeEntry(first, "Apple");
        var e2 = MakeEntry(second, "Apple");

        var result = _comparer.Compare(e1, e2);

        switch (expected)
        {
            case < 0:
                Assert.That(result, Is.LessThan(0));
                break;
            case > 0:
                Assert.That(result, Is.GreaterThan(0));
                break;
            default:
                Assert.That(result, Is.Zero);
                break;
        }
    }

    [TestCase("Apple", "Banana", -1)]
    [TestCase("Banana", "Bahama", 1)]
    [TestCase("Apple", "Apple", 0)]
    public void Compare_DifferentPhrasesAlphabetical(string first, string second, int expected)
    {
        var e1 = MakeEntry(10, first);
        var e2 = MakeEntry(10, second);

        var result = _comparer.Compare(e1, e2);

        switch (expected)
        {
            case < 0:
                Assert.That(result, Is.LessThan(0));
                break;
            case > 0:
                Assert.That(result, Is.GreaterThan(0));
                break;
            default:
                Assert.That(result, Is.Zero);
                break;
        }
    }

    [Test]
    public void Compare_PrefixPhrase()
    {
        var e1 = MakeEntry(5, "Cat");
        var e2 = MakeEntry(6, "Caterpillar");

        var result = _comparer.Compare(e1, e2);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void Compare_EqualEntries()
    {
        var e1 = MakeEntry(123, "Hello");
        var e2 = MakeEntry(123, "Hello");

        var result = _comparer.Compare(e1, e2);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Compare_PhraseCaseMatters()
    {
        var e1 = MakeEntry(1, "apple");
        var e2 = MakeEntry(1, "Apple");

        var result = _comparer.Compare(e1, e2);

        Assert.That(result, Is.EqualTo(0));
    }
}