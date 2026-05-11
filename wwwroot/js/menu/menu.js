
// Lógica de Dropdowns
const dropdownContainers = document.querySelectorAll('.relative');
dropdownContainers.forEach(container => {
    const trigger = container.querySelector('.dropdown-trigger');
    const menu = container.querySelector('.dropdown-menu');
    const selectedText = container.querySelector('.selected-text');
    const arrow = container.querySelector('svg');

    if (!trigger) return;

    trigger.onclick = (e) => {
        e.stopPropagation();
        const wasOpen = menu.classList.contains('show');
        document.querySelectorAll('.dropdown-menu').forEach(m => m.classList.remove('show'));
        document.querySelectorAll('.dropdown-trigger svg').forEach(s => s.style.transform = 'rotate(0deg)');
        if (!wasOpen) {
            menu.classList.add('show');
            arrow.style.transform = 'rotate(180deg)';
        }
    };

    const options = container.querySelectorAll('.option');
    options.forEach(opt => {
        opt.onclick = () => {
            selectedText.innerText = opt.innerText;
            menu.classList.remove('show');
            arrow.style.transform = 'rotate(0deg)';
        };
    });
});

// Cerrar dropdowns al clickear fuera
window.onclick = () => {
    document.querySelectorAll('.dropdown-menu').forEach(m => m.classList.remove('show'));
    document.querySelectorAll('.dropdown-trigger svg').forEach(s => s.style.transform = 'rotate(0deg)');
};

// Sidebar Toggle Mobile
const menuToggle = document.getElementById('menu-toggle');
const sidebar = document.getElementById('sidebar');
const overlay = document.getElementById('sidebar-overlay');

menuToggle.onclick = () => {
    sidebar.classList.add('open');
    overlay.classList.remove('hidden');
};

overlay.onclick = () => {
    sidebar.classList.remove('open');
    overlay.classList.add('hidden');
};