using Android.Content;
using Android.Views;
using AndroidX.Core.Content;
using NetBench.Mobile.Platform;
using Uri = System.Uri;

namespace NetBench.Android;

/// <summary>
/// Android-реализация <see cref="IPlatformServices"/>: «Поделиться» через Intent.ACTION_SEND
/// (файлы — через FileProvider из манифеста), экран — флагом окна, ссылки — ACTION_VIEW.
/// Activity берётся текущая: при пересоздании (тема, конфигурация) она меняется.
/// </summary>
internal sealed class AndroidPlatformServices : IPlatformServices
{
    public Task ShareTextAsync(string text, string title)
    {
        var intent = new Intent(Intent.ActionSend);
        intent.SetType("text/plain");
        intent.PutExtra(Intent.ExtraText, text);
        return StartChooser(intent, title);
    }

    public Task ShareFileAsync(string path, string mimeType, string title)
    {
        var activity = RequireActivity();
        var uri = FileProvider.GetUriForFile(activity, activity.PackageName + ".fileprovider", new Java.IO.File(path))
            ?? throw new InvalidOperationException($"FileProvider did not return a URI for {path}");

        var intent = new Intent(Intent.ActionSend);
        intent.SetType(mimeType);
        intent.PutExtra(Intent.ExtraStream, uri);
        // ClipData — чтобы право чтения досталось и превью в системном меню, а не только получателю.
        intent.ClipData = ClipData.NewRawUri(title, uri);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        return StartChooser(intent, title);
    }

    public void SetKeepScreenOn(bool keepOn)
    {
        if (MainActivity.Current is not { } activity)
            return;

        activity.RunOnUiThread(() =>
        {
            if (keepOn)
                activity.Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
            else
                activity.Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
        });
    }

    public Task<bool> OpenUrlAsync(Uri uri)
    {
        try
        {
            var intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(uri.AbsoluteUri));
            RequireActivity().StartActivity(intent);
            return Task.FromResult(true);
        }
        catch (ActivityNotFoundException)
        {
            return Task.FromResult(false);
        }
    }

    private static Task StartChooser(Intent intent, string title)
    {
        RequireActivity().StartActivity(Intent.CreateChooser(intent, title));
        return Task.CompletedTask;
    }

    private static MainActivity RequireActivity() =>
        MainActivity.Current ?? throw new InvalidOperationException("NetBench activity is not running");
}
