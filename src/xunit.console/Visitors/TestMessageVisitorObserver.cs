using System.Collections.Generic;
using System.Linq;

using Xunit.Abstractions;

namespace Xunit.ConsoleClient.Visitors
{
    /// <summary>
    /// A vistor that allows for multiple subscribers of TestMessageVisitor method calls
    /// </summary>
    public class TestMessageVisitorObserver<T> : TestMessageVisitor<T> where T : IMessageSinkMessage
    {
        private readonly List<TestMessageVisitor> _visitors = new List<TestMessageVisitor>();

        /// <summary>
        /// Creates an instance of a <see cref="TestMessageVisitorObserver{T}"/>
        /// </summary>
        /// <param name="visitors">A set of visitors to subscribe to this object</param>
        public TestMessageVisitorObserver(params TestMessageVisitor[] visitors)
        {
            AddVisitors(visitors);
        }

        /// <summary>
        /// Adds a visitor to sent <see cref="TestMessageVisitor"/> calls to
        /// </summary>
        /// <param name="visitors">A set of visitors to subscribe to this object</param>
        public void AddVisitors(params TestMessageVisitor[] visitors)
        {
            if (visitors != null)
                _visitors.AddRange(visitors.Where(v => v != null));
        }

        /// <summary>
        /// Forwards the message on to all subscribed visitors
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override bool OnMessage(IMessageSinkMessage message)
        {
            foreach (var visitor in _visitors)
                visitor.OnMessage(message);

            return base.OnMessage(message);
        }
    }
}