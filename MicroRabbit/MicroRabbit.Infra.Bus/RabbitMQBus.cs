using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus
    {
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handlers;
        private readonly List<Type> _eventTypes;

        public RabbitMQBus (IMediator mediator)
        {
            _mediator = mediator;
            _handlers = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
        }

        public Task SendCommand<T>(T Command) where T : Command
        {
            return _mediator.Send(Command);
        }

        public void Publish<T>(T @event) where T : Event
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = "localHost"
            };

            using (IConnection connection = factory.CreateConnection())
            using (IModel channel = connection.CreateModel())
            {
                string eventName = @event.GetType().Name;
                channel.QueueDeclare(eventName, false, false, false, null);
                string message = JsonConvert.SerializeObject(@event);
                byte[] body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish("", eventName, null, body);
            }

        }

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
            string eventName = typeof(T).Name;
            Type handlerType = typeof(TH);

            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }

            if (!_handlers.ContainsKey(eventName))
            {
                _handlers.Add(eventName, new List<Type>());
            }

            if (_handlers[eventName].Any(s=> s.GetType() == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already is registerd for '{eventName}'"
                    );
            }

            _handlers[eventName].Add(handlerType);

            StartBasicConsume<T>();
        }

        private void StartBasicConsume<T>() where T : Event
        {
            ConnectionFactory factrory = new ConnectionFactory()
            {
                HostName = "localhost",
                DispatchConsumersAsync = true
            };

            IConnection connection = factrory.CreateConnection();
            IModel channel = connection.CreateModel();

            string eventName = typeof(T).Name;

            channel.QueueDeclare(eventName, false, false, false, null);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += Consumer_Received;

            channel.BasicConsume(eventName,true,consumer);
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            string eventName = e.RoutingKey;
            string message = Encoding.UTF8.GetString(e.Body.ToArray());

            try
            {
                await processEvent(eventName,message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

            }
        }

        private async Task processEvent(string eventName, string message)
        {
            if (_handlers.ContainsKey(eventName))
            {
                List<Type> subscriptions = _handlers[eventName];

                foreach(Type subscription in subscriptions)
                {
                    object handler = Activator.CreateInstance(subscription);

                    if (handler == null)
                    {
                        continue;
                    }
                    Type eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                    object @event = JsonConvert.DeserializeObject(message, eventType);
                    Type concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                }
            }
        }
    }
}
