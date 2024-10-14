$(document).ready(function () {
    AOS.init({ duration: 800, once: true });

    const showLoading = () => {
        if (!$('.loading').length) {
            $('body').append('<div class="loading"><div class="spinner"></div></div>');
        }
    };

    const hideLoading = () => {
        $('.loading').fadeOut(200, function () { $(this).remove(); });
    };

    const showRooms = () => {
        $('.all-rooms-grid').removeClass('hide-rooms').addClass('show-rooms');
    };

    const updateRooms = (page, pushState = true) => {
        showLoading();
        const url = `/${$('input[name="collaboratorCode"]').val()}/get-filtered-rooms`;
        const columns = window.innerWidth < 576 ? 1 : (window.innerWidth < 992 ? 2 : 3);
        const data = {
            districtId: $('#districtFilter').val(),
            priceRange: $('#priceRange').val(),
            roomType: $('#roomType').val(),
            sortOrder: $('#sortOrder').val(),
            page: page || 1,
            columns: columns
        };

        $('html, body').animate({ scrollTop: 0 }, 300);

        $.get(url, data)
            .done(function (result) {
                $('#roomsList').html(result);
                if (pushState) {
                    const newUrl = `/${$('input[name="collaboratorCode"]').val()}/rooms?` + $.param(data);
                    window.history.pushState(data, '', newUrl);
                }
                updateRoomCount();
                initializeLazyLoad();
                initializeTooltips();
                showRooms();
                initLabelEffects();
                AOS.refresh();
            })
            .fail(function (jqXHR, textStatus, errorThrown) {
                console.error('AJAX request failed:', textStatus, errorThrown);
            })
            .always(hideLoading);
    };

    const updateRoomCount = () => {
        const totalFilteredRooms = $('#totalFilteredRooms').data('total');
        $('#filteredRoomCount').text(`(${totalFilteredRooms} phòng)`).addClass('pulse');
        setTimeout(() => $('#filteredRoomCount').removeClass('pulse'), 500);
    };

    const initializeLazyLoad = () => {
        $('img.lazy').Lazy({
            effect: 'fadeIn',
            effectTime: 300,
            afterLoad: function (element) {
                $(element).closest('.all-room-card').find('.skeleton').removeClass('skeleton skeleton-image skeleton-text');
            }
        });
    };

    const initializeTooltips = () => {
        $('[data-toggle="tooltip"]').tooltip();
    };

    const initLabelEffects = () => {
        $('.room-label, .room-status').each(function () {
            $(this).on('mousemove', function (e) {
                const rect = e.target.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                $(this).css('transform', `perspective(100px) rotateX(${(y - rect.height / 2) / 10}deg) rotateY(${-(x - rect.width / 2) / 10}deg) translateY(-3px) rotate(-2deg)`);
            }).on('mouseleave', function () {
                $(this).css('transform', '');
            });
        });
    };

    $('#districtFilter, #priceRange, #roomType, #sortOrder').change(() => updateRooms(1));
    $(document).on('click', '.pagination a', function (e) {
        e.preventDefault();
        updateRooms($(this).data('page'));
    });
    $(window).on('popstate', function (event) {
        if (event.originalEvent.state) {
            updateRooms(event.originalEvent.state.page, false);
        }
    });

    $(window).scroll(function () {
        $('#scrollTopBtn').toggleClass('show', $(this).scrollTop() > 300);
    });
    $('#scrollTopBtn').click(() => {
        $('html, body').animate({ scrollTop: 0 }, 400);
        return false;
    });

    updateRoomCount();
    initializeLazyLoad();
    initializeTooltips();
    showRooms();
    initLabelEffects();

    $('.all-room-card').on('mousemove', function (e) {
        const card = $(this);
        const cardRect = card[0].getBoundingClientRect();
        const mouseX = e.clientX - cardRect.left;
        const mouseY = e.clientY - cardRect.top;
        const rotateY = (mouseX / cardRect.width - 0.5) * 10;
        const rotateX = (mouseY / cardRect.height - 0.5) * -10;

        card.css('transform', `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.02, 1.02, 1.02)`);
    }).on('mouseleave', function () {
        $(this).css('transform', '');
    });

    $('.btn, .page-link').on('click', function (e) {
        const button = $(this);
        const ripple = $('<span class="ripple"></span>');
        button.append(ripple);

        const d = Math.max(button.outerWidth(), button.outerHeight());
        ripple.css({
            height: d,
            width: d,
            top: (e.pageY - button.offset().top - d / 2) + 'px',
            left: (e.pageX - button.offset().left - d / 2) + 'px'
        }).addClass('show');

        setTimeout(() => ripple.remove(), 400);
    });

    $('.form-select').on('change', function () {
        $(this).addClass('selected');
    });

    $(window).on('mousemove', function (e) {
        const moveX = (e.pageX * -1 / 50);
        const moveY = (e.pageY * -1 / 50);
        $('body').css('background-position', `${moveX}px ${moveY}px`);
    });

    // Initialize with current URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    $('#districtFilter').val(urlParams.get('districtId') || '');
    $('#priceRange').val(urlParams.get('priceRange') || '');
    $('#roomType').val(urlParams.get('roomType') || '');
    $('#sortOrder').val(urlParams.get('sortOrder') || '');
    updateRooms(parseInt(urlParams.get('page')) || 1, false);
});