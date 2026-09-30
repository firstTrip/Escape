using System.Collections;
using System.IO;
using ChannelZero.Runtime.Core;
using UnityEngine;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroVisualCaptureBootstrap : MonoBehaviour
    {
        private const string CaptureArgument = "-channelZeroCapture";

        private IEnumerator Start()
        {
            string outputPath = FindCapturePath();
            if (string.IsNullOrWhiteSpace(outputPath))
                yield break;

            yield return null;
            ChannelZeroVerticalSliceController game = FindFirstObjectByType<ChannelZeroVerticalSliceController>();
            ChannelZeroCloseupCanvasController closeup = FindFirstObjectByType<ChannelZeroCloseupCanvasController>();
            if (game == null || closeup == null)
                yield break;

            game.State.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            game.State.Tune(ChannelEra.Year2001);
            closeup.Open(game.State, ChannelZeroIds.LivingNumberRugCloseup);
            ChannelZeroPuzzleService puzzles = new(new ChannelZeroBuiltInPuzzleRegistry(), game.State);
            if (puzzles.TryOpen(ChannelZeroIds.LivingNumberRugCloseup, out PuzzleView view))
                closeup.PresentPuzzle(view);

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Application.persistentDataPath);
            bool captured = CaptureCamera(outputPath);
            Application.Quit(captured ? 0 : 2);
        }

        private static string FindCapturePath()
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
                if (string.Equals(arguments[i], CaptureArgument, System.StringComparison.Ordinal))
                    return Path.GetFullPath(arguments[i + 1]);
            return string.Empty;
        }

        private static bool CaptureCamera(string outputPath)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            RenderTexture target = new(1920, 1080, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new(1920, 1080, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                target.Create();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
                image.Apply(false, false);
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                return File.Exists(outputPath);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Destroy(target);
                Destroy(image);
            }
        }
    }
}
