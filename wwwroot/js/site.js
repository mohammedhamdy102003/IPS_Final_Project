// For SignalR
 


const connection = new signalR.HubConnectionBuilder()
    .withUrl("/ipsHub")
    .withAutomaticReconnect()
    .build();

/*connection.on("ReceiveNewBatch", function (batch) {

    const tbody = document.getElementById("eventsBody");

    if (!tbody)
        return;

    const badgeClass =
        batch.attackType?.toLowerCase() === "benign"
            ? "ben"
            : "mal";

    const statusClass =
        batch.status?.toLowerCase() === "allowed"
            ? "allowed"
            : "blocked";

    const icon =
        statusClass === "blocked"
            ? "fa-ban"
            : "fa-check";

    const row = `
        <tr>
            <td class="mono dim">
                ${new Date(batch.timestamp).toLocaleString()}
            </td>

            <td class="mono ip">
                <a href="#" data-ip="${batch.sourceIp}">
                    ${batch.sourceIp}
                </a>
            </td>

            <td class="mono dim">
                ${batch.destinationIp}
            </td>

            <td>
                ${batch.prediction}
            </td>

            <td>
                <span class="badge ${badgeClass}">
                    ${batch.attackType}
                </span>
            </td>

            <td>
                <div class="pct">
                    ${Math.round(batch.confidence)}%
                </div>

                <div class="bar">
                    <span style="width:${batch.confidence}%"></span>
                </div>
            </td>

            <td>
                <span class="status-pill ${statusClass}">
                    <i class="fa-solid ${icon}"></i>
                    ${batch.status}
                </span>
            </td>
        </tr>
    `;

    tbody.insertAdjacentHTML("afterbegin", row);

    if (tbody.rows.length > 20) {
        tbody.deleteRow(tbody.rows.length - 1);
    }

    // 🔥 تحديث التلت قيم
    updateDashboardStats(batch);
    await refreshPieChart();
});

// update piechart and stats based on the new batch status


async function refreshPieChart() {

    const response =
        await fetch('/DashBoard/GetAttackDistributionData');

    const data = await response.json();

    drawPieChart(data);
}


function updateDashboardStats(batch) {
    // تحديث بناءً على الـ status
    if (batch.status?.toLowerCase() === "blocked") {
        const threatsElement = document.querySelector('[data-stat="threatsBlocked"]');
        if (threatsElement) {
            let current = parseInt(threatsElement.textContent) || 0;
            threatsElement.textContent = current + 1;
        }
    } else if (batch.status?.toLowerCase() === "allowed") {
        const benignElement = document.querySelector('[data-stat="benignTraffic"]');
        if (benignElement) {
            let current = parseInt(benignElement.textContent) || 0;
            benignElement.textContent = current + 1;
        }
    }

    // تحديث Total Events دائماً
    const totalElement = document.querySelector('[data-stat="totalEvents"]');
    if (totalElement) {
        let current = parseInt(totalElement.textContent) || 0;
        totalElement.textContent = current + 1;
    }
}*/

connection.on("ReceiveNewBatch", function (batch) {

    const tbody = document.getElementById("eventsBody");

    if (!tbody)
        return;

    const badgeClass =
        batch.attackType?.toLowerCase() === "benign"
            ? "ben"
            : "mal";

    const statusClass =
        batch.status?.toLowerCase() === "allowed"
            ? "allowed"
            : "blocked";

    const icon =
        statusClass === "blocked"
            ? "fa-ban"
            : "fa-check";

    const row = `
        <tr>
            <td class="mono dim">
                ${new Date(batch.timestamp).toLocaleString()}
            </td>

            <td class="mono ip">
                <a href="#" data-ip="${batch.sourceIp}">
                    ${batch.sourceIp}
                </a>
            </td>

            <td class="mono dim">
                ${batch.destinationIp}
            </td>

            <td>
                ${batch.prediction}
            </td>

            <td>
                <span class="badge ${badgeClass}">
                    ${batch.attackType}
                </span>
            </td>

            <td>
                <div class="pct">
                    ${Math.round(batch.confidence)}%
                </div>

                <div class="bar">
                    <span style="width:${batch.confidence}%"></span>
                </div>
            </td>

            <td>
                <span class="status-pill ${statusClass}">
                    <i class="fa-solid ${icon}"></i>
                    ${batch.status}
                </span>
            </td>
        </tr>
    `;

    tbody.insertAdjacentHTML("afterbegin", row);

    if (tbody.rows.length > 20) {
        tbody.deleteRow(tbody.rows.length - 1);
    }

    // 🔥 تحديث التلت قيم
    updateDashboardStats(batch);

    if (batch.status?.toLowerCase() === "blocked") {
        console.log("🔴 Attack detected - Updating chart");
        refreshAttackChart();
    }
});

// تعريف الدالة إذا ما كانت موجودة في الـ View script
async function refreshAttackChart() {
    try {
        const response = await fetch('/DashBoard/GetAttackDistributionData');

        if (!response.ok) return;

        const data = await response.json();
        renderAttackChart(data);
    } catch (err) {
        console.error("Failed to refresh attack chart:", err);
    }
}

function updateDashboardStats(batch) {
    if (batch.status?.toLowerCase() === "blocked") {
        const threatsElement = document.querySelector('[data-stat="threatsBlocked"]');
        if (threatsElement) {
            let current = parseInt(threatsElement.textContent) || 0;
            threatsElement.textContent = current + 1;
        }
    } else if (batch.status?.toLowerCase() === "allowed") {
        const benignElement = document.querySelector('[data-stat="benignTraffic"]');
        if (benignElement) {
            let current = parseInt(benignElement.textContent) || 0;
            benignElement.textContent = current + 1;
        }
    }

    const totalElement = document.querySelector('[data-stat="totalEvents"]');
    if (totalElement) {
        let current = parseInt(totalElement.textContent) || 0;
        totalElement.textContent = current + 1;
    }
}
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