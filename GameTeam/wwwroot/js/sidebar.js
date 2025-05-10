export async function loadSidebar() {
    const response = await fetch('../pages/Sidebar.html');
    const sidebarHtml = await response.text();
    const placeholder = document.getElementById('sidebar-placeholder');
    if (placeholder) {
        placeholder.innerHTML = sidebarHtml;
    } else {
        console.error('Sidebar placeholder not found');
    }
    await initSidebar();
}

export async function initSidebar() {
    const navLinks = [
        {
            path: '/Questionnaires',
            icon: '../img/questionnaires.svg',
            activeIcon: '../img/questionnaires-active.svg',
        },
        {
            path: '/My_questionnaires',
            icon: '../img/my_questionnaires.svg',
            activeIcon: '../img/my_questionnaires-active.svg',
        },
        {
            path: '/Pending_requests',
            icon: '../img/pending_requests.svg',
            activeIcon: '../img/pending_requests-active.svg',
        },
        {
            path: '/My_teams',
            icon: '../img/teams.svg',
            activeIcon: '../img/teams-active.svg',
        },
    ];

    const currentPath = window.location.pathname.toLowerCase();
    const links = document.querySelectorAll('.sidebar-nav a');

    links.forEach(link => {
        const href = link.getAttribute('href');
        const navIcon = link.querySelector('.nav-icon');

        const isActive = navLinks.some(nav => nav.path.toLowerCase() === href.toLowerCase() && href.toLowerCase() === currentPath);
        if (isActive) {
            link.classList.add('active');
            const activeNav = navLinks.find(nav => nav.path.toLowerCase() === href.toLowerCase());
            if (activeNav && navIcon) {
                navIcon.src = activeNav.activeIcon;
            }
        }

        link.addEventListener('mouseenter', () => {
            const nav = navLinks.find(n => n.path.toLowerCase() === href.toLowerCase());
            if (nav && !link.classList.contains('active') && navIcon) {
                navIcon.src = nav.activeIcon;
            }
        });

        link.addEventListener('mouseleave', () => {
            const nav = navLinks.find(n => n.path.toLowerCase() === href.toLowerCase());
            if (nav && !link.classList.contains('active') && navIcon) {
                navIcon.src = nav.icon;
            }
        });
    });
}