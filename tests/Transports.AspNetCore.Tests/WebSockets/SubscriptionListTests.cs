namespace Tests.WebSockets;

public class SubscriptionListTests
{
    private readonly SubscriptionList _subList;
    private readonly Mock<IDisposable> _mockDisposable = new();
    private IDisposable _disposable => _mockDisposable.Object;

    public SubscriptionListTests()
    {
        _subList = new();
    }

    [Fact]
    public void Dispose_Works()
    {
        _subList.TryAdd("abc", _disposable);
        _subList.Dispose();
        _mockDisposable.Verify(x => x.Dispose(), Times.Once);
        _subList.Dispose();
        _mockDisposable.VerifyNoOtherCalls();
    }

    [Fact]
    public void TryAdd_ThrowsProperly()
    {
        Should.Throw<ArgumentNullException>(() => _subList.TryAdd(null!, _disposable));
        Should.Throw<ArgumentNullException>(() => _subList.TryAdd("abc", null!));
        _subList.Dispose();
        Should.Throw<ObjectDisposedException>(() => _subList.TryAdd("abc", _disposable));
        _mockDisposable.VerifyNoOtherCalls();
    }

    [Fact]
    public void TryAdd_Contains_TryRemove_Works()
    {
        _subList.Contains("abc").ShouldBeFalse();
        _subList.TryAdd("abc", _disposable).ShouldBeTrue();
        _subList.Contains("abc").ShouldBeTrue();
        _subList.TryAdd("abc", Mock.Of<IDisposable>()).ShouldBeFalse();
        _subList.Contains("abc").ShouldBeTrue();
        _subList.TryRemove("abc").ShouldBeTrue();
        _subList.Contains("abc").ShouldBeFalse();
        _subList.TryRemove("abc").ShouldBeFalse();
        _subList.Contains("abc").ShouldBeFalse();
        _mockDisposable.Verify(x => x.Dispose(), Times.Once);
        _mockDisposable.VerifyNoOtherCalls();
    }

    [Fact]
    public void Set_ThrowsProperly()
    {
        Should.Throw<ArgumentNullException>(() => _subList[null!] = _disposable);
        Should.Throw<ArgumentNullException>(() => _subList["abc"] = null!);
        _subList.Dispose();
        Should.Throw<ObjectDisposedException>(() => _subList["abc"] = _disposable);
        _mockDisposable.VerifyNoOtherCalls();
    }

    [Fact]
    public void Set_Contains2_TryRemove2_Works()
    {
        var mockDisposable2 = new Mock<IDisposable>();
        var disposable2 = mockDisposable2.Object;
        _subList.Contains("abc", _disposable).ShouldBeFalse();
        _subList.Contains("abc", disposable2).ShouldBeFalse();
        _subList["abc"] = _disposable;
        _subList.Contains("abc", _disposable).ShouldBeTrue();
        _subList.Contains("abc", disposable2).ShouldBeFalse();
        _subList["abc"] = mockDisposable2.Object;
        _subList.Contains("abc", _disposable).ShouldBeFalse();
        _subList.Contains("abc", disposable2).ShouldBeTrue();
        _mockDisposable.Verify(x => x.Dispose(), Times.Once);
        _subList.TryRemove("abc", _disposable).ShouldBeFalse();
        _subList.Contains("abc", _disposable).ShouldBeFalse();
        _subList.Contains("abc", disposable2).ShouldBeTrue();
        _subList.TryRemove("abc", disposable2).ShouldBeTrue();
        _subList.Contains("abc", _disposable).ShouldBeFalse();
        _subList.Contains("abc", disposable2).ShouldBeFalse();
        mockDisposable2.Verify(x => x.Dispose(), Times.Once);
        _subList.TryRemove("abc", disposable2).ShouldBeFalse();
        _subList.Contains("abc", _disposable).ShouldBeFalse();
        _subList.Contains("abc", disposable2).ShouldBeFalse();
        _mockDisposable.VerifyNoOtherCalls();
        mockDisposable2.VerifyNoOtherCalls();
    }

    [Fact]
    public void Contains_ThrowsProperly()
    {
        Should.Throw<ArgumentNullException>(() => _subList.Contains(null!));
        Should.Throw<ArgumentNullException>(() => _subList.Contains(null!, _disposable));
        Should.Throw<ArgumentNullException>(() => _subList.Contains("abc", null!));
        _subList.Dispose();
        Should.Throw<ObjectDisposedException>(() => _subList.Contains("abc"));
        Should.Throw<ObjectDisposedException>(() => _subList.Contains("abc", _disposable));
        _mockDisposable.VerifyNoOtherCalls();
    }

    [Fact]
    public void TryRemove_ThrowsProperly()
    {
        Should.Throw<ArgumentNullException>(() => _subList.TryRemove(null!));
        Should.Throw<ArgumentNullException>(() => _subList.TryRemove(null!, _disposable));
        Should.Throw<ArgumentNullException>(() => _subList.TryRemove("abc", null!));
        _subList.Dispose();
        Should.Throw<ObjectDisposedException>(() => _subList.TryRemove("abc"));
        Should.Throw<ObjectDisposedException>(() => _subList.TryRemove("abc", _disposable));
        _mockDisposable.VerifyNoOtherCalls();
    }

    [Fact]
    public void CompareExchange_ThrowsProperly()
    {
        Should.Throw<ArgumentNullException>(() => _subList.CompareExchange(null!, _disposable, _disposable));
        Should.Throw<ArgumentNullException>(() => _subList.CompareExchange("abc", null!, _disposable));
        Should.Throw<ArgumentNullException>(() => _subList.CompareExchange("abc", _disposable, null!));
        _subList.Dispose();
        Should.Throw<ObjectDisposedException>(() => _subList.CompareExchange("abc", _disposable, _disposable));
        _mockDisposable.VerifyNoOtherCalls();
    }

    [Fact]
    public void CompareExchange_FalseWhenMissing()
    {
        var mockDisposable2 = new Mock<IDisposable>();
        var disposable2 = mockDisposable2.Object;
        _subList.CompareExchange("abc", _disposable, disposable2).ShouldBeFalse();
        _mockDisposable.VerifyNoOtherCalls();
        mockDisposable2.VerifyNoOtherCalls();
    }

    [Fact]
    public void CompareExchange_FalseWhenNotMatched()
    {
        _subList.TryAdd("abc", _disposable);
        var mockDisposable2 = new Mock<IDisposable>();
        var disposable2 = mockDisposable2.Object;
        var mockDisposable3 = new Mock<IDisposable>();
        var disposable3 = mockDisposable2.Object;
        _subList.CompareExchange("abc", disposable2, disposable3).ShouldBeFalse();
        _mockDisposable.VerifyNoOtherCalls();
        mockDisposable2.VerifyNoOtherCalls();
        mockDisposable3.VerifyNoOtherCalls();
        _subList.Dispose();
        _mockDisposable.Verify(x => x.Dispose());
        _mockDisposable.VerifyNoOtherCalls();
        mockDisposable2.VerifyNoOtherCalls();
        mockDisposable3.VerifyNoOtherCalls();
    }

    [Fact]
    public void CompareExchange_TrueWhenMatched()
    {
        _subList.TryAdd("abc", _disposable);
        var mockDisposable2 = new Mock<IDisposable>();
        var disposable2 = mockDisposable2.Object;
        _subList.CompareExchange("abc", _disposable, disposable2).ShouldBeTrue();
        _mockDisposable.Verify(x => x.Dispose());
        _mockDisposable.VerifyNoOtherCalls();
        mockDisposable2.VerifyNoOtherCalls();
        _subList.Dispose();
        _mockDisposable.VerifyNoOtherCalls();
        mockDisposable2.Verify(x => x.Dispose());
        mockDisposable2.VerifyNoOtherCalls();
    }
}
