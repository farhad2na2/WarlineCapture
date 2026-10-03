# Failed candidate validation

The native builders logged success, but the checked wrapper failed with exit 1 and `reason=domain-reload`. This run does not establish final validation. Unity logged an auto-refresh counter assertion during asset imports. The helper no longer holds that counter across play-mode transitions, and the interrupted temporary save scope was restored while the Editor was idle.

The Editor and Unity Hub were kept running. No licensing recovery, IPC reset or process termination was used.
