import { useEffect, useRef, useState } from "react";

// Minimal shape of the bits of the YouTube IFrame Player API this component actually uses —
// the full type surface isn't published as an npm package, and pulling in a third-party
// @types package for a handful of members isn't worth it.
interface YouTubePlayerInstance {
  getCurrentTime(): number;
  getDuration(): number;
  seekTo(seconds: number, allowSeekAhead: boolean): void;
  destroy(): void;
}

interface YouTubePlayerApi {
  Player: new (
    element: HTMLElement,
    options: {
      videoId: string;
      events: {
        onReady?: () => void;
        onStateChange?: (event: { data: number }) => void;
        onError?: () => void;
      };
    },
  ) => YouTubePlayerInstance;
  PlayerState: { ENDED: number; PLAYING: number; PAUSED: number };
}

declare global {
  interface Window {
    YT?: YouTubePlayerApi;
    onYouTubeIframeAPIReady?: () => void;
  }
}

let apiLoadPromise: Promise<YouTubePlayerApi> | null = null;

/** Loads https://www.youtube.com/iframe_api exactly once, however many players are mounted. */
function loadYouTubeIframeApi(): Promise<YouTubePlayerApi> {
  apiLoadPromise ??= new Promise((resolve) => {
    if (window.YT) {
      resolve(window.YT);
      return;
    }

    const previousCallback = window.onYouTubeIframeAPIReady;
    window.onYouTubeIframeAPIReady = () => {
      previousCallback?.();
      resolve(window.YT!);
    };

    const script = document.createElement("script");
    script.src = "https://www.youtube.com/iframe_api";
    script.async = true;
    document.head.appendChild(script);
  });

  return apiLoadPromise;
}

export interface YouTubePlayerProps {
  videoId: string;
  /** Resume position from a previous session, if any — seeked to once the player is ready. */
  resumeFromSeconds?: number;
  /** Fired every ~15s while playing, and on pause/ended, with the current position/percentage —
   * the caller decides what to do with it (persist progress, auto-complete at 90%, etc.). */
  onProgress: (positionSeconds: number, watchPercentage: number) => void;
  errorMessage: string;
  retryLabel: string;
  watchOnYouTubeLabel: string;
  onPlaybackError: () => void;
}

export function YouTubePlayer({ videoId, resumeFromSeconds, onProgress, errorMessage, retryLabel, watchOnYouTubeLabel, onPlaybackError }: YouTubePlayerProps) {
  // React owns only this stable wrapper. The YouTube API replaces its mount element with an
  // iframe, so giving it the React-owned element directly makes video-to-video navigation race
  // React's cleanup and can throw a removeChild/NotFoundError into the app error boundary.
  const wrapperRef = useRef<HTMLDivElement>(null);
  const onProgressRef = useRef(onProgress);
  onProgressRef.current = onProgress;
  const resumeFromSecondsRef = useRef(resumeFromSeconds);
  resumeFromSecondsRef.current = resumeFromSeconds;
  const [retryNonce, setRetryNonce] = useState(0);
  const [hasPlaybackError, setHasPlaybackError] = useState(false);

  useEffect(() => {
    let player: YouTubePlayerInstance | null = null;
    let intervalId: ReturnType<typeof setInterval> | null = null;
    let cancelled = false;
    const mountElement = document.createElement("div");
    // Captured now, not re-read from wrapperRef.current inside the cleanup below: by the time
    // cleanup runs (e.g. on unmount), the ref may already point elsewhere or be null, which is
    // exactly what react-hooks/exhaustive-deps warns about for a ref read inside a cleanup
    // closure. Capturing the current wrapper element here makes cleanup act on the element this
    // effect actually mounted into, regardless of what the ref holds later.
    const wrapperElement = wrapperRef.current;
    setHasPlaybackError(false);
    wrapperElement?.replaceChildren(mountElement);

    const reportProgress = (target: YouTubePlayerInstance) => {
      const duration = target.getDuration();
      if (!duration) return;
      const position = target.getCurrentTime();
      onProgressRef.current(Math.round(position), (position / duration) * 100);
    };

    void loadYouTubeIframeApi().then((YT) => {
      if (cancelled || !wrapperElement?.contains(mountElement)) return;

      player = new YT.Player(mountElement, {
        videoId,
        events: {
          onReady: () => {
            if (resumeFromSecondsRef.current && resumeFromSecondsRef.current > 0) {
              player?.seekTo(resumeFromSecondsRef.current, true);
            }
          },
          onStateChange: (event) => {
            if (!player) return;

            if (event.data === YT.PlayerState.PLAYING) {
              intervalId ??= setInterval(() => player && reportProgress(player), 15_000);
            } else {
              // Paused, ended, or buffering — stop the periodic timer and report once
              // immediately (covers the "pause"/"complete" progress-reporting triggers).
              if (intervalId) {
                clearInterval(intervalId);
                intervalId = null;
              }
              reportProgress(player);
            }
          },
          onError: () => {
            setHasPlaybackError(true);
            onPlaybackError();
          },
        },
      });
    });

    return () => {
      cancelled = true;
      if (intervalId) clearInterval(intervalId);
      // Report once more on unmount (covers the "navigate away"/"close" triggers).
      if (player) {
        reportProgress(player);
        player.destroy();
      }
      wrapperElement?.replaceChildren();
    };
    // onProgress/resumeFromSeconds are read through refs (onProgressRef/resumeFromSecondsRef)
    // precisely so this effect does not need to depend on them and re-mount the player whenever
    // the caller passes a new function/value — only a real video change (or an explicit retry)
    // should tear down and recreate the YouTube player.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [videoId, retryNonce]);

  return (
    <div className="relative aspect-video w-full overflow-hidden rounded-lg bg-black">
      {/* The YouTube IFrame API injects an <iframe> with its own fixed width/height HTML
          attributes (defaulting to 640x390) regardless of this wrapper's size — [&>iframe]
          overrides those so the video actually fills the responsive aspect-video box instead of
          leaving it mostly empty black space on any viewport wider than 640px. */}
      <div ref={wrapperRef} className="h-full w-full [&>iframe]:h-full [&>iframe]:w-full" />
      {hasPlaybackError && (
        <div className="absolute inset-0 flex flex-col items-center justify-center gap-3 bg-black/90 p-6 text-center text-white">
          <p className="text-sm">{errorMessage}</p>
          <div className="flex flex-wrap justify-center gap-2">
            <button type="button" onClick={() => setRetryNonce((value) => value + 1)} className="min-h-11 rounded-full bg-white px-5 text-sm font-medium text-black">
              {retryLabel}
            </button>
            <a href={`https://www.youtube.com/watch?v=${videoId}`} target="_blank" rel="noreferrer" className="inline-flex min-h-11 items-center rounded-full border border-white px-5 text-sm font-medium text-white">
              {watchOnYouTubeLabel}
            </a>
          </div>
        </div>
      )}
    </div>
  );
}
