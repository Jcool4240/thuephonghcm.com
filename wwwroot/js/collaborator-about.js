var currentImageIndex = 0;
var images = [];

function openModal(imgSrc) {
    currentImageIndex = images.indexOf(imgSrc);
    $('#modalImage').attr('src', imgSrc);
    $('#imageModal').modal('show');
}

function closeModal() {
    $('#imageModal').modal('hide');
}

function changeModalImage(direction) {
    currentImageIndex += direction;
    if (currentImageIndex < 0) {
        currentImageIndex = images.length - 1;
    } else if (currentImageIndex >= images.length) {
        currentImageIndex = 0;
    }
    $('#modalImage').attr('src', images[currentImageIndex]);
}

$(document).ready(function () {
    $('#feedback-form').submit(function (e) {
        e.preventDefault();
        var formData = {
            userName: $('#name').val(),
            email: $('#email').val(),
            phone: $('#phone').val(),
            content: $('#message').val(),
            collaborator_code: window.location.pathname.split('/')[1]
        };

        $.ajax({
            url: 'https://66acec7ef009b9d5c733da23.mockapi.io/mailbox',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function (response) {
                alert("Góp ý của bạn đã được gửi thành công!");
                $('#feedback-form')[0].reset();
            },
            error: function (xhr, status, error) {
                var errorMessage = "Có lỗi xảy ra khi gửi góp ý.";
                alert(errorMessage);
            }
        });
    });


    images = $('.gallery-image').map(function () {
        return $(this).attr('src');
    }).get();

    $(document).keydown(function (e) {
        if ($('#imageModal').hasClass('show')) {
            if (e.key === "ArrowLeft") {
                changeModalImage(-1);
            } else if (e.key === "ArrowRight") {
                changeModalImage(1);
            } else if (e.key === "Escape") {
                closeModal();
            }
        }
    });

    $('.modal-close').click(function () {
        closeModal();
    });
});