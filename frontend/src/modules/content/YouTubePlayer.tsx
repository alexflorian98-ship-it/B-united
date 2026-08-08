import { useEffect, useRef } from "react";

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
}

export function YouTubePlayer({ videoId, resumeFromSeconds, onProgress }: YouTubePlayerProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onProgressRef = useRef(onProgress);
  onProgressRef.current = onProgress;
  const resumeFromSecondsRef = useRef(resumeFromSeconds);
  resumeFromSecondsRef.current = resumeFromSeconds;

  useEffect(() => {
    let player: YouTubePlayerInstance | null = null;
    let intervalId: ReturnType<typeof setInterval> | null = null;
    let cancelled = false;

    const reportProgress = (target: YouTubePlayerInstance) => {
      const duration = target.getDuration();
      if (!duration) return;
      const position = target.getCurrentTime();
      onProgressRef.current(Math.round(position), (position / duration) * 100);
    };

    void loadYouTubeIframeApi().then((YT) => {
      if (cancelled || !containerRef.current) return;

      player = new YT.Player(containerRef.current, {
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
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [videoId]);

  return <div ref={containerRef} className="aspect-video w-full overflow-hidden rounded-lg bg-black" />;
}
