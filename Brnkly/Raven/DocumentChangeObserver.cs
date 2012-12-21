using System;
using Raven.Abstractions.Data;

namespace Brnkly.Raven
{
    public class DocumentChangeObserver : IObserver<DocumentChangeNotification>
    {
        private static Action NoOp = () => { };
        private static Action<Exception> Throw =
            ex => { throw new Exception("An error occured in the observable.", ex); };

        private Action<DocumentChangeNotification> onChange;
        private Action<Exception> onError;
        private Action onCompleted;

        public DocumentChangeObserver(
            Action<DocumentChangeNotification> onNext,
            Action<Exception> onError = null,
            Action onCompleted = null)
        {
            onNext.Ensure("changeHandler").IsNotNull();

            this.onChange = onNext;
            this.onError = onError ?? Throw;
            this.onCompleted = onCompleted ?? NoOp;
        }

        public void OnCompleted()
        {
            this.onCompleted();
        }

        public void OnError(Exception error)
        {
            this.onError(error);
        }

        public void OnNext(DocumentChangeNotification value)
        {
            this.onChange(value);
        }
    }
}
