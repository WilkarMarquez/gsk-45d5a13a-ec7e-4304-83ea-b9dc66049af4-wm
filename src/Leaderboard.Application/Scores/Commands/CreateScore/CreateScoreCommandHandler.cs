using Leaderboard.Application.IRepositories;
using Leaderboard.Domain.Entities;
using MediatR;

namespace Leaderboard.Application.Scores.Commands.CreateScore;

public class CreateScoreCommandHandler
    : IRequestHandler<CreateScoreCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IScoreRepository _scoreRepository;
    private readonly IUserAggregateRepository _userAggregateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateScoreCommandHandler(
        IUserRepository userRepository,
        IScoreRepository scoreRepository,
        IUserAggregateRepository userAggregateRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _scoreRepository = scoreRepository;
        _userAggregateRepository = userAggregateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(
        CreateScoreCommand request,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = await _userRepository.GetByExternalIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    ExternalId = request.UserId,
                    Name = request.Name,
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user, cancellationToken);
            }

            await _scoreRepository.AddAsync(new ScoreEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Score = request.Score,
                EventTimestamp = request.Timestamp,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            var aggregate = await _userAggregateRepository.GetAsync(
                user.Id,
                cancellationToken);

            if (aggregate == null)
            {
                aggregate = new UserAggregate
                {
                    UserId = user.Id,
                    TotalScore = request.Score,
                    LastUpdated = DateTime.UtcNow
                };

                await _userAggregateRepository.AddAsync(
                    aggregate,
                    cancellationToken);
            }
            else
            {
                aggregate.TotalScore += request.Score;
                aggregate.LastUpdated = DateTime.UtcNow;

                await _userAggregateRepository.UpdateAsync(
                    aggregate,
                    cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}