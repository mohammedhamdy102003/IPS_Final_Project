// For SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/ipsHub")
    .withAutomaticReconnect()
    .build();


connection.on("ReceiveAttackAlert", function (notification) {
    console.log("SignalR: هجوم جديد مكتشف!", notification);

    const badge = document.getElementById('notiBadge');
    if (badge) {
        let currentCount = parseInt(badge.innerText) || 0;
        badge.innerText = currentCount + 1;


        badge.style.display = 'flex';
        badge.style.opacity = '1';
        badge.style.pointerEvents = 'auto';
        badge.classList.remove('hidden-badge');
        badge.classList.add('pulse-animation');
    }


    const notiItems = document.getElementById('notiItems');
    if (notiItems) {
        const newItem = document.createElement('div');
        newItem.className = 'noti-item';
        newItem.style = "padding: 12px; border-bottom: 1px solid var(--border); display: flex; justify-content: space-between; align-items: center;";
        newItem.innerHTML = `
            <div>
                <p style="margin:0; font-size: 13px; color: var(--red); font-weight: bold;">${notification.attackType}</p>
                <small style="color: var(--muted);">${notification.sourceIp} - Just Now</small>
            </div>
            <div style="display: flex; gap: 5px;">
                <button onclick="showDetails(${notification.id})" class="read-more-btn" style="background:none; border:1px solid var(--blue); color:var(--blue); font-size:10px; padding:2px 5px; border-radius:4px; cursor:pointer;">Read More</button>
            </div>
        `;
        if (notiItems.querySelector('.empty-noti')) notiItems.innerHTML = '';
        notiItems.prepend(newItem);
    }
});

connection.on("ReceiveRefresh", () => location.reload());

connection.start()
    .then(() => console.log("SignalR: Connected!"))
    .catch(err => console.error(err.toString()));


document.addEventListener('DOMContentLoaded', function () {
    const notiBtn = document.getElementById('notiBtn');
    const notiBadge = document.getElementById('notiBadge');
    const notiDropdown = document.getElementById('notiDropdown');

    if (notiBtn) {
        notiBtn.addEventListener('click', async function (e) {
            e.preventDefault();
            e.stopPropagation();

            const isOpening = notiDropdown.classList.contains('hidden');

            if (isOpening) {

                notiDropdown.classList.remove('hidden');


                if (notiBadge) {
                    notiBadge.style.opacity = '0';
                    notiBadge.classList.remove('pulse-animation');

                    setTimeout(() => {
                        notiBadge.style.display = 'none';
                        notiBadge.innerText = "0";
                    }, 300);
                }

                try {
                    await fetch('/api/Traffic/MarkAsRead', { method: 'POST' });
                    console.log("Database: Status sync complete.");
                } catch (err) {
                    console.error("Failed to sync read status", err);
                }
            } else {
                notiDropdown.classList.add('hidden');
            }
        });
    }


    document.addEventListener('click', function (e) {
        if (notiDropdown && !notiDropdown.contains(e.target) && !notiBtn.contains(e.target)) {
            notiDropdown.classList.add('hidden');
        }
    });
});


async function showDetails(id) {
    try {
        const res = await fetch(`/api/Traffic/GetNotificationDetails/${id}`);
        if (!res.ok) throw new Error("Could not fetch details");
        const data = await res.json();

        const modalBody = document.getElementById('modalBody');
        modalBody.innerHTML = `
            <div style="color: white; display: grid; grid-template-columns: 1fr 1fr; gap: 15px; font-size: 14px;">
                <p><strong>Attack:</strong> <span style="color:#ef4444">${data.attackType}</span></p>
                <p><strong>Source IP:</strong> ${data.sourceIp}</p>
                <p><strong>Target IP:</strong> ${data.destinationIp}</p>
                <p><strong>Confidence:</strong> ${data.confidence}%</p>
                <p><strong>Protocol:</strong> ${data.protocol}</p>
                <p><strong>Time:</strong> ${new Date(data.timestamp).toLocaleString()}</p>
            </div>
        `;
        document.getElementById('detailsModal').classList.remove('hidden');
    } catch (err) {
        console.error("Error loading modal details:", err);
    }
}

function closeModal() {
    const modal = document.getElementById('detailsModal');
    if (modal) modal.classList.add('hidden');
}

async function apiDeleteOne(id) {
    const res = await fetch(`/api/Traffic/DeleteNotification/${id}`, { method: 'DELETE' });
    if (res.ok) location.reload();
}

async function apiClearAll() {
    if (confirm("Are you sure you want to clear all notifications?")) {
        const res = await fetch(`/api/Traffic/ClearAllNotifications`, { method: 'DELETE' });
        if (res.ok) location.reload();
    }
}

function downloadReport() {
    window.open('/api/Reports/DownloadLogsPdf', '_blank');
}


/*
   Export Logs Filtering
 */


function toggleCustomDates() {
    const rangeSelect = document.getElementById('reportRange');
    const customDiv = document.getElementById('customDateInputs');

    if (rangeSelect && customDiv) {

        if (rangeSelect.value === 'custom') {
            customDiv.style.display = 'flex';
        } else {
            customDiv.style.display = 'none';
        }
    }
}


function downloadFilteredReport() {

    const rangeElement = document.getElementById('reportRange');
    const startElement = document.getElementById('startDate');
    const endElement = document.getElementById('endDate');

    if (!rangeElement) {
        console.error("Element 'reportRange' not found!");
        return;
    }

    const range = rangeElement.value;
    let url = `/api/Reports/DownloadLogsPdf?range=${range}`;

    if (range === 'custom') {
        const startValue = startElement ? startElement.value : "";
        const endValue = endElement ? endElement.value : "";


        console.log("Start Date:", startValue, "End Date:", endValue);

        if (!startValue || !endValue) {
            alert("⚠️ Please select both Start and End dates.");
            return;
        }
        url += `&start=${startValue}&end=${endValue}`;
    }


    window.open(url, '_blank');
}


function toggleCustomDates() {
    const rangeSelect = document.getElementById('reportRange');
    const customDiv = document.getElementById('customDateInputs');

    if (rangeSelect && customDiv) {

        customDiv.style.display = (rangeSelect.value === 'custom') ? 'flex' : 'none';
    }
}

function downloadFilteredReport() {
    const rangeElement = document.getElementById('reportRange');
    const startInput = document.getElementById('startDate');
    const endInput = document.getElementById('endDate');

    if (!rangeElement) {
        console.error("Element 'reportRange' not found!");
        return;
    }

    const range = rangeElement.value;
    let url = `/api/Reports/DownloadLogsPdf?range=${range}`;

    if (range === 'custom') {
        const start = startInput.value;
        const end = endInput.value;

        if (!start || !end) {
            alert("⚠️ Please select both Start and End dates.");
            return;
        }


        if (new Date(start) > new Date(end)) {
            alert("⚠️ Start date cannot be later than End date.");
            return;
        }

        url += `&start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}`;
    }


    console.log("Requesting Report from:", url);


    const downloadWindow = window.open(url, '_blank');


    if (!downloadWindow) {
        window.location.href = url;
    }
}