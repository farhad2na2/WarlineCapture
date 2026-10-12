(() => {
  "use strict";

  // Replace with the live listing URLs before publishing.
  const STORE_URLS = {
    ios: "https://apps.apple.com/app/warline-capture/id0000000000",
    android: "https://play.google.com/store/apps/details?id=com.warlinecapture.game",
  };

  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // ---------- Store links ----------
  const ua = navigator.userAgent || "";
  const isIOS = /iPad|iPhone|iPod/.test(ua) || (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
  const isAndroid = /Android/i.test(ua);
  const platform = isIOS ? "ios" : isAndroid ? "android" : null;

  document.querySelectorAll("a[data-store]").forEach((link) => {
    const store = link.dataset.store === "auto" ? platform : link.dataset.store;
    if (!store) return;
    link.href = STORE_URLS[store];
    link.target = "_blank";
    if (platform && store === platform && link.classList.contains("store-btn")) {
      link.classList.add("is-primary");
      link.parentElement.prepend(link);
    }
  });

  // ---------- Header + mobile nav ----------
  const header = document.querySelector("[data-header]");
  const nav = document.querySelector("[data-nav]");
  const toggle = document.querySelector("[data-nav-toggle]");

  const setNavOpen = (open) => {
    nav.classList.toggle("is-open", open);
    toggle.setAttribute("aria-expanded", String(open));
    document.body.style.overflow = open ? "hidden" : "";
  };

  toggle.addEventListener("click", () => setNavOpen(!nav.classList.contains("is-open")));
  nav.addEventListener("click", (event) => {
    if (event.target.closest("a")) setNavOpen(false);
  });
  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && nav.classList.contains("is-open")) {
      setNavOpen(false);
      toggle.focus();
    }
  });

  const onScroll = () => header.classList.toggle("is-solid", window.scrollY > 24);
  onScroll();
  window.addEventListener("scroll", onScroll, { passive: true });

  // ---------- Sticky download bar ----------
  const sticky = document.querySelector("[data-sticky-cta]");
  const hero = document.querySelector("[data-hero]");
  const download = document.getElementById("download");
  if (sticky && hero && "IntersectionObserver" in window) {
    const visible = { hero: true, download: false };
    const update = () => {
      const show = !visible.hero && !visible.download;
      sticky.classList.toggle("is-visible", show);
      sticky.setAttribute("aria-hidden", String(!show));
      sticky.querySelector("a").tabIndex = show ? 0 : -1;
    };
    const watch = (element, key) =>
      new IntersectionObserver(([entry]) => {
        visible[key] = entry.isIntersecting;
        update();
      }).observe(element);
    watch(hero, "hero");
    watch(download, "download");
  }

  // ---------- Reveal on scroll ----------
  const revealItems = document.querySelectorAll(".reveal");
  if (reduceMotion || !("IntersectionObserver" in window)) {
    revealItems.forEach((item) => item.classList.add("is-visible"));
  } else {
    const revealObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add("is-visible");
          revealObserver.unobserve(entry.target);
        });
      },
      { threshold: 0.15, rootMargin: "0px 0px -40px 0px" }
    );
    revealItems.forEach((item) => revealObserver.observe(item));
  }

  // ---------- Stat counters ----------
  const counters = document.querySelectorAll("[data-count]");
  if (!reduceMotion && "IntersectionObserver" in window) {
    const countObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        countObserver.unobserve(entry.target);
        const target = Number(entry.target.dataset.count);
        const start = performance.now();
        const duration = 1400;
        const tick = (now) => {
          const progress = Math.min((now - start) / duration, 1);
          const eased = 1 - Math.pow(1 - progress, 3);
          entry.target.textContent = String(Math.round(target * eased));
          if (progress < 1) requestAnimationFrame(tick);
        };
        entry.target.textContent = "0";
        requestAnimationFrame(tick);
      });
    }, { threshold: 0.6 });
    counters.forEach((counter) => countObserver.observe(counter));
  }

  // ---------- ARIA typewriter ----------
  const typewriter = document.querySelector("[data-typewriter]");
  if (typewriter && !reduceMotion) {
    const lines = typewriter.dataset.lines.split("|");
    let lineIndex = 0;
    const typeLine = () => {
      const text = lines[lineIndex];
      let charIndex = 0;
      typewriter.textContent = "";
      typewriter.classList.add("is-typing");
      const step = () => {
        typewriter.textContent = text.slice(0, ++charIndex);
        if (charIndex < text.length) {
          setTimeout(step, 32);
          return;
        }
        const isLast = lineIndex === lines.length - 1;
        lineIndex = (lineIndex + 1) % lines.length;
        setTimeout(typeLine, isLast ? 4200 : 1500);
      };
      step();
    };
    setTimeout(typeLine, 600);
  }

  // ---------- Chapter carousel ----------
  const carousel = document.querySelector("[data-carousel]");
  if (carousel) {
    const track = carousel.querySelector("[data-carousel-track]");
    const cards = Array.from(track.children);
    const dots = carousel.querySelector("[data-carousel-dots]");
    const behavior = reduceMotion ? "auto" : "smooth";

    const scrollToCard = (index) => {
      const clamped = Math.max(0, Math.min(index, cards.length - 1));
      const offset = cards[clamped].offsetLeft - cards[0].offsetLeft;
      track.scrollTo({ left: offset, behavior });
    };

    const currentIndex = () => {
      const base = cards[0].offsetLeft;
      let best = 0;
      cards.forEach((card, index) => {
        const distance = Math.abs(card.offsetLeft - base - track.scrollLeft);
        if (distance < Math.abs(cards[best].offsetLeft - base - track.scrollLeft)) best = index;
      });
      return best;
    };

    const dotButtons = cards.map((card, index) => {
      const button = document.createElement("button");
      button.type = "button";
      button.setAttribute("aria-label", `Chapter ${index + 1}`);
      button.addEventListener("click", () => scrollToCard(index));
      dots.appendChild(button);
      return button;
    });

    const syncDots = () => {
      const atEnd = track.scrollLeft + track.clientWidth >= track.scrollWidth - 4;
      const active = atEnd ? cards.length - 1 : currentIndex();
      dotButtons.forEach((button, index) => button.setAttribute("aria-current", String(index === active)));
    };

    let scrollFrame = 0;
    track.addEventListener("scroll", () => {
      cancelAnimationFrame(scrollFrame);
      scrollFrame = requestAnimationFrame(syncDots);
    }, { passive: true });

    carousel.querySelector("[data-carousel-prev]").addEventListener("click", () => scrollToCard(currentIndex() - 1));
    carousel.querySelector("[data-carousel-next]").addEventListener("click", () => scrollToCard(currentIndex() + 1));
    track.addEventListener("keydown", (event) => {
      if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
      event.preventDefault();
      scrollToCard(currentIndex() + (event.key === "ArrowRight" ? 1 : -1));
    });
    syncDots();
  }

  // ---------- Footer year ----------
  const year = document.querySelector("[data-year]");
  if (year) year.textContent = String(new Date().getFullYear());
})();
