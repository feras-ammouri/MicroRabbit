using MicroRabbit.Domain.Core.Events;
using System.Threading.Tasks;

namespace MicroRabbit.Domain.Core.Bus
{
    public interface IEventHandler<in T> : IEventHandler 
        where T : Event
    {
        Task Handle(T @event);
    }

    public interface IEventHandler
    {

    }
}
