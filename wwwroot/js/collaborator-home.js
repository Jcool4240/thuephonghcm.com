document.addEventListener('DOMContentLoaded', (event) => {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            document.querySelector(this.getAttribute('href')).scrollIntoView({
                behavior: 'smooth'
            });
        });
    });

    const districtItems = document.querySelectorAll('.district-item');
    districtItems.forEach(item => {
        item.addEventListener('mouseenter', () => {
            item.style.transform = 'translateY(-5px)';
        });
        item.addEventListener('mouseleave', () => {
            item.style.transform = 'translateY(0)';
        });
    });

    const contactIcons = document.querySelectorAll('.contact-icon');
    contactIcons.forEach(icon => {
        icon.addEventListener('mouseenter', () => {
            icon.style.transform = 'rotate(360deg)';
        });
        icon.addEventListener('mouseleave', () => {
            icon.style.transform = 'rotate(0deg)';
        });
    });

    const sections = document.querySelectorAll('.section');
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('appear');
            }
        });
    }, { threshold: 0.1 });

    sections.forEach(section => {
        observer.observe(section);
    });

    const roomCounts = document.querySelectorAll('.room-count');
    roomCounts.forEach(count => {
        const finalValue = parseInt(count.textContent);
        let currentValue = 0;
        const duration = 1000;
        const stepTime = 50;
        const steps = duration / stepTime;
        const increment = finalValue / steps;

        const timer = setInterval(() => {
            currentValue += increment;
            if (currentValue >= finalValue) {
                clearInterval(timer);
                count.textContent = `${finalValue} `;
            } else {
                count.textContent = `${Math.floor(currentValue)} `;
            }
        }, stepTime);
    });

    const labels = document.querySelectorAll('.label');
    labels.forEach(label => {
        const labelText = label.querySelector('.label-text');
        if (labelText) {
            if (label.classList.contains('label-VIP-Ultimate')) {
                labelText.textContent = 'VIP ♾️';
            } else if (label.classList.contains('label-VIP-Plus')) {
                labelText.textContent = 'VIP +';
            }
        }
    });

    function adjustDistrictItems() {
        const districtItems = document.querySelectorAll('.district-item');
        districtItems.forEach(item => {
            const districtName = item.querySelector('.district-name');
            const roomCount = item.querySelector('.room-count');
            if (districtName && roomCount) {
                const availableWidth = item.offsetWidth - 40;
                const nameWidth = districtName.offsetWidth;
                const countWidth = roomCount.offsetWidth;

                if (nameWidth + countWidth > availableWidth) {
                    districtName.style.maxWidth = `${availableWidth - countWidth - 10}px`;
                } else {
                    districtName.style.maxWidth = '';
                }
            }
        });
    }

    window.addEventListener('load', adjustDistrictItems);
    window.addEventListener('resize', adjustDistrictItems);
});

