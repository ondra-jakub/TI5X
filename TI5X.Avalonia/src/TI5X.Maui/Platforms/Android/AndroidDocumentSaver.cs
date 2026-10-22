using Android.App;
using Android.Content;
using Microsoft.Maui.ApplicationModel;

namespace TI5X.Maui;

public static class AndroidDocumentSaver
{
    public const int RequestCode = 0x5449;

    private static TaskCompletionSource<Stream?>? _pending;

    public static Task<Stream?> CreateAsync(string fileName, string mimeType)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null)
        {
            return Task.FromResult<Stream?>(null);
        }

        var tcs = new TaskCompletionSource<Stream?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref _pending, tcs)?.TrySetResult(null);

        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType(mimeType);
        intent.PutExtra(Intent.ExtraTitle, fileName);
        activity.StartActivityForResult(intent, RequestCode);

        return tcs.Task;
    }

    internal static void HandleResult(int requestCode, Android.App.Result resultCode, Intent? data)
    {
        if (requestCode != RequestCode) return;

        var tcs = Interlocked.Exchange(ref _pending, null);
        if (tcs == null) return;

        if (resultCode != Android.App.Result.Ok || data?.Data == null)
        {
            tcs.TrySetResult(null);
            return;
        }

        try
        {
            var stream = Platform.AppContext.ContentResolver?.OpenOutputStream(data.Data, "wt");
            tcs.TrySetResult(stream);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }
}
