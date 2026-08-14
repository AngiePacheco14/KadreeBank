using FluentAssertions;
using KadreeBank.Application.Services;
using KadreeBank.Domain.Entities;
using KadreeBank.Domain.Enums;
using KadreeBank.Domain.Exceptions;
using KadreeBank.Domain.Interfaces;
using Moq;
using Xunit;

namespace KadreeBank.UnitTests.Application.Services;

public class CustomerServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Customers).Returns(_customerRepository.Object);
        _sut = new CustomerService(_unitOfWork.Object);
    }

    [Fact]
    public async Task CreateCustomerAsync_PersistsCustomerAndReturnsDto()
    {
        var result = await _sut.CreateCustomerAsync(CustomerType.Natural, "Ana Pérez", "CC-123");

        result.FullName.Should().Be("Ana Pérez");
        result.Type.Should().Be(CustomerType.Natural);
        _customerRepository.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ThrowsNotFound()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerExists_ReturnsDto()
    {
        var customer = new Customer { Type = CustomerType.Business, FullName = "Acme S.A.S", DocumentNumber = "NIT-999" };
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await _sut.GetByIdAsync(customer.Id);

        result.Id.Should().Be(customer.Id);
        result.FullName.Should().Be("Acme S.A.S");
    }

    [Fact]
    public async Task GetAllAsync_WithoutTypeFilter_ReturnsAllCustomersMappedToDto()
    {
        var customers = new List<Customer>
        {
            new() { Type = CustomerType.Natural, FullName = "Ana Pérez", DocumentNumber = "CC-123" },
            new() { Type = CustomerType.Business, FullName = "Acme S.A.S", DocumentNumber = "NIT-999" }
        };
        _customerRepository.Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(customers);

        var result = await _sut.GetAllAsync(null);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithTypeFilter_PassesFilterThroughToRepository()
    {
        var naturalCustomers = new List<Customer>
        {
            new() { Type = CustomerType.Natural, FullName = "Ana Pérez", DocumentNumber = "CC-123" }
        };
        _customerRepository.Setup(r => r.GetAllAsync(CustomerType.Natural, It.IsAny<CancellationToken>()))
            .ReturnsAsync(naturalCustomers);

        var result = await _sut.GetAllAsync(CustomerType.Natural);

        result.Should().ContainSingle().Which.Type.Should().Be(CustomerType.Natural);
    }
}
