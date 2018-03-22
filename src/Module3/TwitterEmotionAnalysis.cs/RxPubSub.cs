using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace RxPublisherSubscriber
{
    //  Reactive Publisher Subscriber in C#
    public class RxPubSub<T> : IDisposable, ISubject<T>
    {
        private ISubject<T> subject;
        private readonly Func<T, bool> filter;
        private List<IObserver<T>> observers = new List<IObserver<T>>();
        private List<IDisposable> observables = new List<IDisposable>();

        public RxPubSub(ISubject<T> subject, Func<T, bool> filter = null)
        {
            this.subject = subject;
            this.filter = filter ?? new Func<T, bool>(_ => true);
        }
        public RxPubSub() : this(new Subject<T>()) { }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observers.Add(observer);
            subject.Subscribe(observer);
            return new ObserverHandler<T>(observer, observers);
        }

        public IDisposable AddPublisher(IObservable<T> observable) =>
            observable.SubscribeOn(TaskPoolScheduler.Default).Subscribe(subject);

        public IObservable<T> AsObservable() =>
                subject.AsObservable().Where(filter);

        public void Dispose()
        {
            observers.ForEach(x => x.OnCompleted());
            observers.Clear();
        }

        public void OnNext(T value) => subject.OnNext(value);
        public void OnError(Exception error) => subject.OnError(error);

        public void OnCompleted() => subject.OnCompleted();
    }

    class ObserverHandler<T> : IDisposable
    {
        private IObserver<T> observer;
        private List<IObserver<T>> observers;

        public ObserverHandler(IObserver<T> observer, List<IObserver<T>> observers)
        {
            this.observer = observer;
            this.observers = observers;
        }

        public void Dispose()
        {
            observer.OnCompleted();
            observers.Remove(observer);
        }
    }
}
