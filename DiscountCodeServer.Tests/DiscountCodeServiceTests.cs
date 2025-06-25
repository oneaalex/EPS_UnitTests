using Moq;
using DiscountCodeServer.Services;
using DiscountCodeServer.Repositories;
using DiscountCodeServer.Models;
using DiscountCodeServer.UnitOfWork;

namespace DiscountCodeServer.Tests
{
    public class DiscountCodeServiceTests
    {
        [Fact]
        public async Task GenerateAndAddCodesAsync_Success_ReturnsTrue()
        {
            // Arrange
            var mockRepo = new Mock<IDiscountCodeRepository>();
            var mockGen = new Mock<IDiscountCodeGenerator>();
            var mockUow = new Mock<IUnitOfWork>();

            mockRepo.Setup(r => r.GetAllCodesAsync()).ReturnsAsync([]);
            mockGen.Setup(g => g.GenerateCodes(2, 8, It.IsAny<List<string>>()))
                   .Returns(["CODE1", "CODE2"]);

            var addedCodes = new List<DiscountCode>();
            mockRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<DiscountCode>>()))
                    .Callback<IEnumerable<DiscountCode>>(codes => addedCodes.AddRange(codes))
                    .Returns(Task.CompletedTask);

            mockUow.Setup(u => u.CompleteAsync()).Returns(Task.CompletedTask);

            var service = new DiscountCodeService(mockRepo.Object, mockGen.Object, mockUow.Object);

            // Act
            var result = await service.GenerateAndAddCodesAsync(2, 8);

            // Assert
            Assert.True(result);
            Assert.Equal(2, addedCodes.Count);
            mockUow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateAndAddCodesAsync_RepositoryThrows_ReturnsFalse()
        {
            // Arrange
            var mockRepo = new Mock<IDiscountCodeRepository>();
            var mockGen = new Mock<IDiscountCodeGenerator>();
            var mockUow = new Mock<IUnitOfWork>();

            mockRepo.Setup(r => r.GetAllCodesAsync()).ReturnsAsync([]);
            mockGen.Setup(g => g.GenerateCodes(1, 8, It.IsAny<List<string>>()))
                   .Returns(["CODE1"]);
            mockRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<DiscountCode>>()))
                    .ThrowsAsync(new Exception("DB error"));

            var service = new DiscountCodeService(mockRepo.Object, mockGen.Object, mockUow.Object);

            // Act
            var result = await service.GenerateAndAddCodesAsync(1, 8);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UseCodeAsync_Success_ReturnsSuccess()
        {
            // Arrange
            var mockRepo = new Mock<IDiscountCodeRepository>();
            var mockGen = new Mock<IDiscountCodeGenerator>();
            var mockUow = new Mock<IUnitOfWork>();

            var code = new DiscountCode { Code = "CODE1", IsUsed = false };
            mockRepo.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(code);
            mockRepo.Setup(r => r.UpdateAsync(code)).Returns(Task.CompletedTask);
            mockUow.Setup(u => u.CompleteAsync()).Returns(Task.CompletedTask);

            var service = new DiscountCodeService(mockRepo.Object, mockGen.Object, mockUow.Object);

            // Act
            var result = await service.UseCodeAsync("CODE1");

            // Assert
            Assert.Equal((byte)UseCodeResultEnum.Success, result);
            Assert.True(code.IsUsed);
            mockRepo.Verify(r => r.UpdateAsync(code), Times.Once);
            mockUow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UseCodeAsync_AlreadyUsed_ReturnsFailure()
        {
            // Arrange
            var mockRepo = new Mock<IDiscountCodeRepository>();
            var mockGen = new Mock<IDiscountCodeGenerator>();
            var mockUow = new Mock<IUnitOfWork>();

            var code = new DiscountCode { Code = "CODE1", IsUsed = true };
            mockRepo.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(code);

            var service = new DiscountCodeService(mockRepo.Object, mockGen.Object, mockUow.Object);

            // Act
            var result = await service.UseCodeAsync("CODE1");

            // Assert
            Assert.Equal((byte)UseCodeResultEnum.Failure, result);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<DiscountCode>()), Times.Never);
            mockUow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task UseCodeAsync_NotFound_ReturnsFailure()
        {
            // Arrange
            var mockRepo = new Mock<IDiscountCodeRepository>();
            var mockGen = new Mock<IDiscountCodeGenerator>();
            var mockUow = new Mock<IUnitOfWork>();

            // Fix for CS8620: Ensure nullability matches the expected type
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            mockRepo.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync((DiscountCode?)null);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

            var service = new DiscountCodeService(mockRepo.Object, mockGen.Object, mockUow.Object);

            // Act
            var result = await service.UseCodeAsync("CODE1");

            // Assert
            Assert.Equal((byte)UseCodeResultEnum.Failure, result);
        }

        [Fact]
        public async Task UseCodeAsync_RepositoryThrows_ReturnsException()
        {
            // Arrange
            var mockRepo = new Mock<IDiscountCodeRepository>();
            var mockGen = new Mock<IDiscountCodeGenerator>();
            var mockUow = new Mock<IUnitOfWork>();

            mockRepo.Setup(r => r.GetByCodeAsync("CODE1")).ThrowsAsync(new Exception("DB error"));

            var service = new DiscountCodeService(mockRepo.Object, mockGen.Object, mockUow.Object);

            // Act
            var result = await service.UseCodeAsync("CODE1");

            // Assert
            Assert.Equal((byte)UseCodeResultEnum.Exception, result);
        }
    }
}