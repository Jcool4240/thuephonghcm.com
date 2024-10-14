$(document).ready(function () {
    // Kiểm tra xem Select2 đã được tải chưa
    if ($.fn.select2) {
        $('.select2').select2({
            theme: 'bootstrap4',
            placeholder: 'Chọn một',
            allowClear: true
        });
    } else {
        console.error("Select2 is not loaded");
    }

    // Kiểm tra xem Summernote đã được tải chưa
    if ($.fn.summernote) {
        $('#summernote').summernote({
            height: 200,
            toolbar: [
                ['style', ['style']],
                ['font', ['bold', 'underline', 'clear']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture']],
                ['view', ['fullscreen', 'codeview', 'help']]
            ]
        });
    } else {
        console.error("Summernote is not loaded");
    }

    // Format giá phòng
    function formatPrice(input) {
        let value = input.value.replace(/\D/g, '');
        if (value === '') return;
        value = parseInt(value, 10);
        input.value = value.toLocaleString('vi-VN');
    }

    $('.price-input').on('input', function () {
        formatPrice(this);
    });

    // Xử lý hiển thị ảnh
    function readURL(input) {
        if (input.files && input.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var imgElement = $(input).siblings('img');
                imgElement.attr('src', e.target.result);
                imgElement.show();
                $(input).siblings('label').hide();
                $(input).siblings('.remove-image').show();
            }
            reader.readAsDataURL(input.files[0]);
        }
    }

    $(".image-upload input[type='file']").change(function () {
        readURL(this);
    });

    $(".remove-image").click(function (e) {
        e.preventDefault();
        var container = $(this).closest('.image-upload');
        container.find('input[type="file"]').val('');
        container.find('img').attr('src', '#').hide();
        container.find('label').show();
        $(this).hide();
    });

    $("#addRoomForm").submit(function (e) {
        e.preventDefault();
        var formData = new FormData(this);

        var priceValue = $('.price-input').val().replace(/\D/g, '');
        formData.set('PriceRoom', priceValue);

        // Kiểm tra xem có hình chính không
        var mainImage = $('#MainImageFile')[0].files[0];
        if (!mainImage) {
            Swal.fire({
                icon: 'error',
                title: 'Lỗi!',
                text: 'Vui lòng chọn ảnh chính.',
                confirmButtonText: 'OK'
            });
            return;
        }

        $.ajax({
            url: $(this).attr('action'),
            type: 'POST',
            data: formData,
            success: function (result) {
                if (result.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Thành công!',
                        text: result.message,
                        confirmButtonText: 'OK'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            window.location.href = '/' + collaboratorCode + '/manager/rooms';
                        }
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi!',
                        text: result.message || 'Có lỗi xảy ra khi thêm phòng. Vui lòng thử lại.',
                        confirmButtonText: 'OK'
                    });
                }
            },
            error: function (xhr, status, error) {
                if (xhr.status === 401) {
                    window.location.href = '/jcool-dev-room/login?ReturnUrl=' + encodeURIComponent(window.location.pathname);
                } else {
                    console.error("Error:", xhr.responseText);
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi!',
                        text: 'Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại.',
                        confirmButtonText: 'OK'
                    });
                }
            },
            cache: false,
            contentType: false,
            processData: false
        });
    });

    // Format giá ban đầu nếu có
    formatPrice($('.price-input')[0]);

    // Log để kiểm tra
    console.log("Script loaded successfully");
    $(".image-upload input[type='file']").each(function () {
        console.log("File input found:", this.id);
    });
});