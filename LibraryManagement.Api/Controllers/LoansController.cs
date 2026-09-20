using LibraryManagement.Api.Common;
using LibraryManagement.Api.DTOs.Books;
using LibraryManagement.Api.DTOs.Loans;
using LibraryManagement.Api.RequestParameters;
using LibraryManagement.Api.RequestParameters.Enums;
using LibraryManagement.Api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController(
    ILoansService loansService) : WebApiController
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReadLoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadLoanDto>> GetById(int id)
    {
        var result = await loansService.GetByIdAsync(id);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ReadLoanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReadLoanDto>>> GetAll(
        [FromQuery] PaginationParameters pagination,
        [FromQuery] LoanFilteringParameters filteringParameters,
        [FromQuery] SortingParameters<LoanSortBy> sortingParameters)
    {
        var result = await loansService.GetAllAsync(
            pagination,
            filteringParameters,
            sortingParameters);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        Response.Headers.Append(ResponseHeaders.TotalCount, result.Value.TotalCount.ToString());
        Response.Headers.Append(ResponseHeaders.TotalPages, result.Value.TotalPages.ToString());

        return Ok(result.Value.Items);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReadLoanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReadLoanDto>> Create(CreateLoanDto createDto)
    {
        var result = await loansService.CreateAsync(createDto);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.Id },
            result.Value);
    }

    [HttpPatch("{id:int}/return")]
    [ProducesResponseType(typeof(ReadLoanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReadLoanDto>> Return(int id)
    {
        var result = await loansService.ReturnAsync(id);

        if (result.IsError)
        {
            return Problem(result.FirstError);
        }

        return Ok(result.Value);
    }
}
