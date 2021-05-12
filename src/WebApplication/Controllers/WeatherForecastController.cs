using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebApplication.Controllers
{
    public interface IHandler<in TCommand, TResponse>
    {
        Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
    }

    public class Command
    {
        public Command(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public class Unit
    {
    }

    public class Handler : IHandler<Command, Unit>
    {
        private readonly ILogger<Handler> _logger;

        public Handler(ILogger<Handler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public Task<Unit> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@Command} processing", command);
            return Task.FromResult(new Unit());
        }
    }

    public class DecoratorHandler<TCommand, TResponse> : IHandler<TCommand, TResponse>
    {
        private readonly ILogger<DecoratorHandler<TCommand,TResponse>> _logger;
        private readonly IHandler<TCommand,TResponse> _handler;

        public DecoratorHandler(IHandler<TCommand, TResponse> handler, ILogger<DecoratorHandler<TCommand, TResponse>> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        public async Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("{@Command} start executing", command);
                var result = await _handler.HandleAsync(command, cancellationToken);
                _logger.LogInformation("{@Command} executed with {@Result}", command, result);
                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error happened");
                throw;
            }
        }
    }
    
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IHandler<Command, Unit> _handler;

        public WeatherForecastController(IHandler<Command, Unit> handler)
        {
            _handler = handler;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var command = new Command("Message");
            await _handler.HandleAsync(command, cancellationToken);
            return Ok();
        }
    }
}