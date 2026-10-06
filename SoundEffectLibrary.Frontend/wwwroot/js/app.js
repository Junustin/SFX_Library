const soundList = document.querySelector(".sound-list");
const paginationButton = document.querySelector(".pagination-button");
const searchBar = document.querySelector(".search-bar");
const searchButton = document.querySelector(".search-button");

const loadingMessage = document.getElementById("loading-message");
const errorMessage = document.getElementById("error-message");
const errorText = document.querySelector(".error-text");
const tryagainButton = document.querySelector(".tryagain-button")

const apiUrl = `http://localhost:8080`;

// Audio player
let currentAudio = null;
let currentButton = null;
let currentProgressBar = null;
let currentTimeDisplay = null;
let audio = null;

// Pagination
let currentPage = 1;
let pageSize = 5;
let pageCount = 1;

// Search
let currentSearchString = "";

// Loading/Error


loadAssets(1, currentSearchString);


searchButton.addEventListener(`click`, () => {
    startSearch();
});

tryagainButton.addEventListener(`click`, () => {
    loadAssets(currentPage, currentSearchString);
});

async function loadAssets(page, search) {
    try {
        loadingMessage.hidden = false;
        errorMessage.hidden = true;

        const response = await fetch(`${apiUrl}/api/audioassets?Search=${search}&Page=${currentPage}&PageSize=${pageSize}`);
        if (!response.ok) {
            throw new Error(`HTTP Error: ${response.status}`)
        }

        const data = await response.json();

        soundList.innerHTML = "";
        paginationButton.innerHTML = "";

        if (currentAudio != null)
            currentAudio.pause();

        // Create each card
        data.items.forEach(asset => {

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
                    `${apiUrl}` + asset.previewFileUrl
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

                const fileUrl = `${apiUrl}/api/audioassets/${asset.id}/files`;

                const link = document.createElement('a');
                link.href = fileUrl;

                link.setAttribute('download', ``);

                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

            });

            soundList.appendChild(card);
        });

        pageCount = calculatePage(data.totalCount);

        // Create page controller button
        paginationButton.innerHTML = `
                    <div class="pagination">
                        <button ${currentPage === 1 ? "disabled" : ""} class="previous-button">Previous</button>

                        <span class="page-info">Page ${currentPage} of ${pageCount}</span>

                        <button ${currentPage === pageCount ? "disabled" : ""} class="next-button">Next</button>
                    </div>
    `;

        const nextButton = paginationButton.querySelector(".next-button");
        const previousButton = paginationButton.querySelector(".previous-button");

        nextButton.addEventListener(`click`, () => {
            nextPage();
        });
        previousButton.addEventListener(`click`, () => {
            previousPage();
        });
    }
    catch (ex) {
        console.log("Error");
        loadingMessage.hidden = true;
        errorMessage.hidden = false;
        errorText.textContent = ex.message;
        console.log(ex);
    }
    finally {
        loadingMessage.hidden = true;
    }
}

function startSearch() {
    currentPage = 1;

    // Update search string
    currentSearchString = searchBar.value;

    loadAssets(1, currentSearchString);
}
function nextPage() {
    if (currentPage + 1 <= pageCount) {
        currentPage += 1;
        loadAssets(currentPage, currentSearchString);
    }
}
function previousPage() {
    if (currentPage > 1) {
        currentPage -= 1;
        loadAssets(currentPage, currentSearchString);
    }
}
function calculatePage(totalCount) {
    pageCount = Math.ceil(totalCount / pageSize);
    return pageCount;
}
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
