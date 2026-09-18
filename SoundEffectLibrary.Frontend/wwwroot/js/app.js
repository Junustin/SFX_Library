const soundList = document.querySelector(".sound-list");
let currentAudio = null;
let currentButton = null;
let currentProgressBar = null;
let currentTimeDisplay = null;

fetch("https://localhost:7096/api/audioassets")
    .then(response => response.json())
    .then(data => {
        data.forEach(asset => {

            const card = document.createElement("article");

            card.classList.add("sound-card");

            card.innerHTML = `
                    <div class="sound-info">
                        <h3>${asset.title}</h3>
                        <p>${asset.categoryResponse.categoryName}</p>
                    </div>
                    <div class="sound-actions">
                        <button class="play-button">Play</button>

                        <input
                            class="progress-bar"
                            type="range"
                            min="0"
                            max="100"
                            value="0"
                        > 

                        <span class="time-display">0:00 / 0:00</span>

                        <button class="download-button">Download</button>
                    </div>
                `;

            const playButton = card.querySelector(".play-button");
            const progressBar = card.querySelector(".progress-bar");
            const timeDisplay = card.querySelector(".time-display");
            const downloadButton = card.querySelector(".download-button")

            progressBar.addEventListener("input", () => {
                if (audio !== null && audio.duration) {
                        audio.currentTime =
                        (progressBar.value / 100) * audio.duration;
                 }
            });

            playButton.addEventListener("click", () => {

                // This card is already the currently playing card.
                if (currentAudio !== null && currentButton === playButton) {

                    if (currentAudio.paused) {
                        currentAudio.play();
                        playButton.textContent = "Pause";
                    } else {
                        currentAudio.pause();
                        playButton.textContent = "Play";
                    }

                    return;
                }

                // Another card is currently playing.
                if (currentAudio !== null && currentButton !== playButton) {
                    currentAudio.pause();
                    currentAudio.currentTime = 0;

                    currentButton.textContent = "Play";

                    currentProgressBar.value = 0;

                    currentTimeDisplay.textContent =
                        `0:00 / ${formatTime(currentAudio.duration)}`;
                }

                // Create audio for this card.
                audio = new Audio(
                    "https://localhost:7096" + asset.previewFileUrl
                );

                audio.addEventListener("loadedmetadata", () => {
                    timeDisplay.textContent =
                        `0:00 / ${formatTime(audio.duration)}`;
                });

                audio.addEventListener("timeupdate", () => {
                    if (!Number.isFinite(audio.duration)) {
                        return;
                    }

                    progressBar.value =
                        (audio.currentTime / audio.duration) * 100;

                    timeDisplay.textContent =
                        `${formatTime(audio.currentTime)} / ${formatTime(audio.duration)}`;
                });

                audio.addEventListener("ended", () => {
                    playButton.textContent = "Play";

                    progressBar.value = 0;

                    timeDisplay.textContent =
                        `0:00 / ${formatTime(audio.duration)}`;

                    // Only clear the global state if this is
                    // still the currently active audio.
                    if (currentAudio === audio) {
                        currentAudio = null;
                        currentButton = null;
                        currentProgressBar = null;
                        currentTimeDisplay = null;
                    }
                });

                audio.play();

                currentAudio = audio;
                currentButton = playButton;
                currentProgressBar = progressBar;
                currentTimeDisplay = timeDisplay;

                playButton.textContent = "Pause";
            });

            downloadButton.addEventListener("click", () => {

                const fileUrl = `https://localhost:7096/api/audioassets/${asset.id}/files`;

                const link = document.createElement('a');
                link.href = fileUrl;

                link.setAttribute('download', ``);

                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

            });

            soundList.appendChild(card);  
        });
    });
function formatTime(seconds) {
    if (!Number.isFinite(seconds)) {
        return "0:00";
    }

    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.floor(seconds % 60);

    return `${minutes}:${remainingSeconds
        .toString()
        .padStart(2, "0")}`;
}
