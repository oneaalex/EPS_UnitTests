using Moq;
using Microsoft.AspNetCore.SignalR;
using DiscountCodeServer.Controllers;
using DiscountCodeServer.Services;

namespace DiscountCodeServer.Tests;
public enum UseCodeResultEnum : byte
{
    Failure = 0,
    Success = 1,
    Exception = 2
}

public class DiscountCodeHubTests
{
    [Fact]
    public async Task GenerateCode_Success_SendsCodeGenerated()
    {
        // Arrange
        var mockService = new Mock<IDiscountCodeService>();
        mockService.Setup(s => s.GenerateAndAddCodesAsync(1, 8)).ReturnsAsync(true);

        var mockClients = new Mock<IHubCallerClients>();
        var mockAll = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockAll.Object);

        var hub = new DiscountCodeHub(mockService.Object)
        {
            Clients = mockClients.Object,
            Context = Mock.Of<HubCallerContext>(c => c.ConnectionId == "test-conn")
        };

        // Act
        await hub.GenerateCode(1, 8);

        // Assert
        mockService.Verify(s => s.GenerateAndAddCodesAsync(1, 8), Times.Once);
        mockAll.Verify(a => a.SendCoreAsync("CodeGenerated",
    It.Is<object[]>(o => (bool)o[0] == true),
    It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCode_2000_SendsCodeGenerated()
    {
        // Arrange
        var mockService = new Mock<IDiscountCodeService>();
        mockService.Setup(s => s.GenerateAndAddCodesAsync(2000, 8)).ReturnsAsync(true);

        var mockClients = new Mock<IHubCallerClients>();
        var mockAll = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockAll.Object);

        var hub = new DiscountCodeHub(mockService.Object)
        {
            Clients = mockClients.Object,
            Context = Mock.Of<HubCallerContext>(c => c.ConnectionId == "test-conn")
        };

        // Act
        await hub.GenerateCode(2000, 8);

        // Assert
        mockService.Verify(s => s.GenerateAndAddCodesAsync(2000, 8), Times.Once);
        mockAll.Verify(a => a.SendCoreAsync(
            "CodeGenerated",
            It.Is<object[]>(o => (bool)o[0] == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UseCode_Success_SendsCodeUsed()
    {
        // Arrange
        var mockService = new Mock<IDiscountCodeService>();
        mockService.Setup(s => s.UseCodeAsync("ABC")).ReturnsAsync((byte)UseCodeResultEnum.Success);

        var mockClients = new Mock<IHubCallerClients>();
        var mockAll = new Mock<IClientProxy>();
        var mockCaller = new Mock<ISingleClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockAll.Object);
        mockClients.Setup(c => c.Caller).Returns(mockCaller.Object);

        var hub = new DiscountCodeHub(mockService.Object)
        {
            Clients = mockClients.Object,
            Context = Mock.Of<HubCallerContext>(c => c.ConnectionId == "test-conn")
        };

        // Act
        await hub.UseCode("ABC");

        // Assert
        mockService.Verify(s => s.UseCodeAsync("ABC"), Times.Once);
        // Verify the correct message and payload
        mockAll.Verify(a => a.SendCoreAsync(
    "CodeUsed",
    It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "ABC"),
    It.IsAny<CancellationToken>()),
    Times.Once);
    }

    [Fact]
    public async Task UseCode_Failure_SendsError()
    {
        // Arrange
        var mockService = new Mock<IDiscountCodeService>();
        mockService.Setup(s => s.UseCodeAsync("XYZ")).ReturnsAsync((byte)UseCodeResultEnum.Failure);

        var mockClients = new Mock<IHubCallerClients>();
        var mockAll = new Mock<IClientProxy>();
        var mockCaller = new Mock<ISingleClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockAll.Object);
        mockClients.Setup(c => c.Caller).Returns(mockCaller.Object);

        var hub = new DiscountCodeHub(mockService.Object)
        {
            Clients = mockClients.Object,
            Context = Mock.Of<HubCallerContext>(c => c.ConnectionId == "test-conn")
        };

        // Act
        await hub.UseCode("XYZ");

        // Assert
        mockService.Verify(s => s.UseCodeAsync("XYZ"), Times.Once);
        // Verify the SendAsync call on 'Caller' for the 'Error' message
        mockCaller.Verify(c => c.SendCoreAsync(
    "Error",
    It.Is<object[]>(o => o.Length == 1 && (string)o[0] == "Failed to use code"),
    It.IsAny<CancellationToken>()), Times.Once);
    }
}
