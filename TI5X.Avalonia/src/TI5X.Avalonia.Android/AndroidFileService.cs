using Android.App;
using Android.Content;
using Android.Provider;
using Android.Net;
using TI5X.Avalonia.Services;

namespace TI5X.Avalonia.Android;

/// <summary>
/// Android (SAF) implementation of <see cref="IAppFileService"/> — port of the MAUI
/// AndroidDocumentSaver. Uses TaskCompletionSource slots handed to
/// <see cref="MainActivity"/> via <see cref="Avalonia.Android.MainActivity.HandleActivityResult"/>.
/// Opened input is buffered into a seekable MemoryStream (zip container needs seeking);
/// created output is opened with the "wt" (truncate) mode.
/// </summary>
public class AndroidFileService(Activity activity) : IAppFileService
{
    private const int OpenRequestCode = 0x5449;
    private const int CreateRequestCode = 0x544A;

    private TaskCompletionSource<OpenedFile?>? _pendingOpen;
    private TaskCompletionSource<Stream?>? _pendingCreate;

    public Task<OpenedFile?> OpenAsync(string title, IEnumerable<string> patterns, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<OpenedFile?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref _pendingOpen, tcs)?.TrySetResult(null);

        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetTypeAndNormalize("*/*");
        activity.StartActivityForResult(intent, OpenRequestCode);

        return tcs.Task;
    }

    public Task<Stream?> CreateAsync(string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<Stream?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Exchange(ref _pendingCreate, tcs)?.TrySetResult(null);

        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetTypeAndNormalize(mimeType);
        intent.PutExtra(Intent.ExtraTitle, fileName);
        activity.StartActivityForResult(intent, CreateRequestCode);

        return tcs.Task;
    }

    public void HandleResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode == OpenRequestCode)
        {
            var openTcs = Interlocked.Exchange(ref _pendingOpen, null);
            if (openTcs == null) return;

            if (resultCode != Result.Ok || data?.Data == null)
            {
                openTcs.TrySetResult(null);
                return;
            }

            try
            {
                openTcs.TrySetResult(OpenBuffered(activity, data.Data));
            }
            catch (Exception ex)
            {
                openTcs.TrySetException(ex);
            }
        }
        else if (requestCode == CreateRequestCode)
        {
            var createTcs = Interlocked.Exchange(ref _pendingCreate, null);
            if (createTcs == null) return;

            if (resultCode != Result.Ok || data?.Data == null)
            {
                createTcs.TrySetResult(null);
                return;
            }

            try
            {
                // "wt" = truncate-and-write, so an existing file gets replaced.
                createTcs.TrySetResult(activity.ContentResolver?.OpenOutputStream(data.Data, "wt"));
            }
            catch (Exception ex)
            {
                createTcs.TrySetException(ex);
            }
        }
    }

    private static OpenedFile OpenBuffered(Context context, global::Android.Net.Uri uri)
    {
        using var input = context.ContentResolver!.OpenInputStream(uri)
            ?? throw new IOException($"Could not open input stream for {uri}");
        var buffer = new MemoryStream();
        input.CopyTo(buffer);
        buffer.Position = 0;
        return new OpenedFile(buffer, GetDisplayName(context, uri) ?? uri.LastPathSegment);
    }

    private static string? GetDisplayName(Context context, global::Android.Net.Uri uri)
    {
        try
        {
            using var cursor = context.ContentResolver?.Query(uri, null, null, null, null);
            if (cursor == null) return null;

            var nameIndex = cursor.GetColumnIndex(IOpenableColumns.DisplayName);
            if (nameIndex >= 0 && cursor.MoveToFirst())
            {
                return cursor.GetString(nameIndex);
            }
        }
        catch (Exception)
        {
            // Fall back to the last path segment below.
        }

        return null;
    }
}