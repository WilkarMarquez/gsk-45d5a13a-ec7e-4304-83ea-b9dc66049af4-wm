using Leaderboard.Application.Leaderboard.Queries.GetLeaderboard;
using Leaderboard.Application.Scores.Commands.CreateScore;
using Leaderboard.Application.Users.Queries.GetUserScore;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Leaderboard.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class LeaderboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaderboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("score")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateScore(
        [FromBody] CreateScoreCommand command,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);

        return Created();
    }

    [HttpGet("users/{userId}/score")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserScore(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetUserScoreQuery(userId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("leaderboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetLeaderboardQuery(top),
            cancellationToken);

        return Ok(result);
    }
}