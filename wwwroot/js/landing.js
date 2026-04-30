// =============================
// 1️⃣ Loader - شاشة التحميل
// =============================
window.addEventListener("load", () => {
    setTimeout(() => {
        const loader = document.getElementById("loader");
        if (loader) loader.style.display = "none";
    }, 1200);
});

// =============================
// 2️⃣ Counter - العداد
// =============================
let count = 0;
let target = 2431;
let counter = setInterval(() => {
    count += 7;
    if (count >= target) {
        count = target;
        clearInterval(counter);
    }
    const threatElem = document.getElementById("threat-number");
    if (threatElem) threatElem.innerText = count.toLocaleString();
}, 10);

// =============================
// 3️⃣ Network Background - 
// =============================
const canvas = document.getElementById("network");
const ctx = canvas.getContext("2d");

function resizeCanvas() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}
resizeCanvas();

let particles = [];
for (let i = 0; i < 120; i++) {
    particles.push({
        x: Math.random() * canvas.width,
        y: Math.random() * canvas.height,
        vx: (Math.random() - 0.5) * 0.5,
        vy: (Math.random() - 0.5) * 0.5
    });
}

function animateNetwork() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    particles.forEach(p => {
        p.x += p.vx;
        p.y += p.vy;

        if (p.x < 0 || p.x > canvas.width) p.vx *= -1;
        if (p.y < 0 || p.y > canvas.height) p.vy *= -1;

        ctx.beginPath();
        ctx.arc(p.x, p.y, 1.5, 0, Math.PI * 2);
        ctx.fillStyle = "rgba(0,255,255,0.5)";
        ctx.fill();

        particles.forEach(p2 => {
            let dist = Math.hypot(p.x - p2.x, p.y - p2.y);
            if (dist < 150) {
                ctx.beginPath();
                ctx.moveTo(p.x, p.y);
                ctx.lineTo(p2.x, p2.y);
                ctx.strokeStyle = `rgba(0, 210, 255, ${0.15 - dist / 1000})`;
                ctx.stroke();
            }
        });
    });
    requestAnimationFrame(animateNetwork);
}
animateNetwork();

// =============================
// 4️⃣ THREE JS GLOBE 
// =============================
// =============================
// 4️⃣ THREE JS GLOBE (FIXED)
// =============================

const globeDiv = document.getElementById("globe");

let scene, camera, renderer, earth, glow;

if (globeDiv) {

    scene = new THREE.Scene();

    const size = globeDiv.clientWidth;

    camera = new THREE.PerspectiveCamera(45, 1, 0.1, 1000);
    camera.position.z = 5;

    renderer = new THREE.WebGLRenderer({
        alpha: true,
        antialias: true
    });

    renderer.setSize(size, size);
    renderer.setPixelRatio(window.devicePixelRatio);

    globeDiv.innerHTML = "";
    globeDiv.appendChild(renderer.domElement);

    // Geometry
    const geometry = new THREE.SphereGeometry(2, 64, 64);

    // Texture
    const texture = new THREE.TextureLoader().load("earth_dark2.jpg");

    // Material
    const material = new THREE.MeshStandardMaterial({
        map: texture,
        emissive: 0x003344,
        emissiveIntensity: 0.7,
        roughness: 0.6,
        metalness: 0.3
    });

    earth = new THREE.Mesh(geometry, material);
    scene.add(earth);

    // Glow
    const glowGeo = new THREE.SphereGeometry(2.15, 64, 64);
    const glowMat = new THREE.MeshBasicMaterial({
        color: 0x00ffff,
        transparent: true,
        opacity: 0.12
    });

    glow = new THREE.Mesh(glowGeo, glowMat);
    scene.add(glow);

    // Lights
    scene.add(new THREE.AmbientLight(0x003344, 1.4));

    const light = new THREE.PointLight(0x00ffff, 2);
    light.position.set(3, 2, 4);
    scene.add(light);

    function animate() {
        earth.rotation.y += 0.0025;
        glow.rotation.y += 0.0025;

        renderer.render(scene, camera);
        requestAnimationFrame(animate);
    }

    animate();
}


// =============================
// 5️⃣ Resize Fix
// =============================
window.addEventListener("resize", () => {

    resizeCanvas();

    if (renderer && globeDiv) {
        const newSize = globeDiv.clientWidth;
        renderer.setSize(newSize, newSize);
        camera.aspect = 1;
        camera.updateProjectionMatrix();
    }

});