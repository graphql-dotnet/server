using GraphQLParser.AST;

namespace Tests;

public class FormFileGraphTypeTests
{
    private static readonly FormFileGraphType _scalar = new();
    private static readonly IFormFile _formFile = Mock.Of<IFormFile>();
    private static readonly byte[] _byteArray = [1, 2, 3];
    private static readonly string _base64 = Convert.ToBase64String(_byteArray);

    [Fact]
    public void Name()
    {
        _scalar.Name.ShouldBe("FormFile");
    }

    [Fact]
    public void Serialize_Null()
    {
        _scalar.Serialize(null).ShouldBeNull();
    }

    [Fact]
    public void Serialize_IFormFile()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.Serialize(_formFile));
    }

    [Fact]
    public void Serialize_ByteArray()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.Serialize(_byteArray));
    }

    [Fact]
    public void Serialize_Base64()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.Serialize(_base64));
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
        var literal = new GraphQLStringValue(_base64);
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
        _scalar.CanParseValue(_formFile).ShouldBeTrue();
        _scalar.ParseValue(_formFile).ShouldBe(_formFile);
    }

    [Fact]
    public void ParseValue_ByteArray()
    {
        _scalar.CanParseValue(_byteArray).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _scalar.ParseValue(_byteArray));
    }

    [Fact]
    public void ParseValue_Base64()
    {
        _scalar.CanParseValue(_base64).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _scalar.ParseValue(_base64));
    }

    [Fact]
    public void IsValidDefault_Null()
    {
        _scalar.IsValidDefault(null!).ShouldBeFalse();
    }

    [Fact]
    public void IsValidDefault_FormFile()
    {
        _scalar.IsValidDefault(_formFile).ShouldBeFalse();
    }

    [Fact]
    public void IsValidDefault_ByteArray()
    {
        _scalar.IsValidDefault(_byteArray).ShouldBeFalse();
    }

    [Fact]
    public void IsValidDefault_Base64()
    {
        _scalar.IsValidDefault(_base64).ShouldBeFalse();
    }

    [Fact]
    public void ToAST_Null()
    {
        _scalar.ToAST(null).ShouldBeOfType<GraphQLNullValue>();
    }

    [Fact]
    public void ToAST_FormFile()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.ToAST(_formFile));
    }

    [Fact]
    public void ToAST_ByteArray()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.ToAST(_byteArray));
    }

    [Fact]
    public void ToAST_Base64()
    {
        Should.Throw<InvalidOperationException>(() => _scalar.ToAST(_base64));
    }
}
