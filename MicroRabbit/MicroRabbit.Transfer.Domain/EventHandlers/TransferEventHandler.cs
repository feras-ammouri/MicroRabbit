using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Transfer.Domain.Events;
using MicroRabbit.Transfer.Domain.Interfaces;
using System.Threading.Tasks;

namespace MicroRabbit.Transfer.Domain.EventHandlers
{
    public class TransferEventHandler : IEventHandler<TransferCreatedEvent>
    {
        private readonly ITransferRepository _transferRepository;

        public TransferEventHandler(ITransferRepository transferRepository)
        {
            _transferRepository = transferRepository;
        }

        public Task Handle(TransferCreatedEvent @event)
        {
            return Task.CompletedTask;
        }
    }
}
