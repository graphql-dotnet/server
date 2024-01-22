using GraphQLParser.AST;

namespace Tests;

public class FormFileGraphTypeTests
{
    private readonly FormFileGraphType _scalar = new();
    private readonly IFormFile _dummy = Mock.Of<IFormFile>();

    [Fact]
    public void Serialize_Null()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.Serialize(null));
    }

    [Fact]
    public void Serialize_IFormFile()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.Serialize(_dummy));
    }

    [Fact]
    public void Serialize_ByteArray()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.Serialize(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void Serialize_Base64()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.Serialize(Convert.ToBase64String(new byte[] { 1, 2, 3 })));
    }

    [Fact]
    public void ParseLiteral_Null()
    {
        _scalar.CanParseLiteral(new GraphQLNullValue()).ShouldBeTrue();
        _scalar.ParseLiteral(new GraphQLNullValue()).ShouldBeNull();
    }

    [Fact]
    public void ParseLiteral_Base64()
    {
        var literal = new GraphQLStringValue(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        _scalar.CanParseLiteral(literal).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _scalar.ParseLiteral(literal));
    }

    [Fact]
    public void ParseLiteral_ByteArray()
    {
        var literal = new GraphQLListValue() { Values = new() { new GraphQLIntValue(1) } };
        _scalar.CanParseLiteral(literal).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _scalar.ParseLiteral(literal));
    }

    [Fact]
    public void ParseValue_Null()
    {
        _scalar.CanParseValue(null).ShouldBeTrue();
        _scalar.ParseValue(null).ShouldBeNull();
    }

    [Fact]
    public void ParseValue_IFormFile()
    {
        _scalar.CanParseValue(_dummy).ShouldBeTrue();
        _scalar.ParseValue(_dummy).ShouldBe(_dummy);
    }

    [Fact]
    public void ParseValue_ByteArray()
    {
        _scalar.CanParseValue(new byte[] { 1, 2, 3 }).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _scalar.ParseValue(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void ParseValue_Base64()
    {
        _scalar.CanParseValue(Convert.ToBase64String(new byte[] { 1, 2, 3 })).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _scalar.ParseValue(Convert.ToBase64String(new byte[] { 1, 2, 3 })));
    }
}
