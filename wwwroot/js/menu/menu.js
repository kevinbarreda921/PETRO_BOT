// Función para inicializar o refrescar los eventos del menú
window.initMenu = () => {
    // Delegación de eventos para Dropdowns (funciona aunque Blazor recree el HTML)
    document.addEventListener('click', (e) => {
        // Lógica para Triggers de Dropdown
        const trigger = e.target.closest('.dropdown-trigger');
        if (trigger) {
            e.stopPropagation();
            const container = trigger.closest('.relative');
            const menu = container.querySelector('.dropdown-menu');
            const arrow = trigger.querySelector('svg');

            const wasOpen = menu.classList.contains('show');

            // Cerrar otros
            document.querySelectorAll('.dropdown-menu').forEach(m => m.classList.remove('show'));
            document.querySelectorAll('.dropdown-trigger svg').forEach(s => s.style.transform = 'rotate(0deg)');

            if (!wasOpen) {
                menu.classList.add('show');
                if (arrow) arrow.style.transform = 'rotate(180deg)';
            }
            return;
        }

        // Lógica para Opciones del Dropdown
        const option = e.target.closest('.option');
        if (option) {
            const container = option.closest('.relative');
            const selectedText = container.querySelector('.selected-text');
            const menu = container.querySelector('.dropdown-menu');
            const arrow = container.querySelector('.dropdown-trigger svg');

            if (selectedText) selectedText.innerText = option.innerText;
            menu.classList.remove('show');
            if (arrow) arrow.style.transform = 'rotate(0deg)';
            return;
        }

        // Lógica para Sidebar Toggle
        const menuToggle = e.target.closest('#menu-toggle');
        if (menuToggle) {
            document.getElementById('sidebar')?.classList.add('open');
            document.getElementById('sidebar-overlay')?.classList.remove('hidden');
            return;
        }

        // Lógica para Overlay
        if (e.target.id === 'sidebar-overlay') {
            document.getElementById('sidebar')?.classList.remove('open');
            e.target.classList.add('hidden');
            return;
        }

        // Cerrar todo al hacer click fuera
        document.querySelectorAll('.dropdown-menu').forEach(m => m.classList.remove('show'));
        document.querySelectorAll('.dropdown-trigger svg').forEach(s => s.style.transform = 'rotate(0deg)');
    });
};

// Ejecutar al cargar
window.initMenu();