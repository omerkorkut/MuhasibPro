using Microsoft.UI.Dispatching;

namespace MuhasibPro
{
    public static class DispatcherQueueExtensions
    {
        public static Task EnqueueAsync(this DispatcherQueue dispatcherQueue, Action action)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            if (!dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    taskCompletionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            }))
            {
                taskCompletionSource.SetException(new InvalidOperationException("DispatcherQueue.TryEnqueue failed"));
            }

            return taskCompletionSource.Task;
        }
    }
}
