$(document).ready(function () {
    // Kiểm tra và khởi tạo Select2
    if ($.fn.select2) {
        $('.select2').select2({
            theme: 'bootstrap4',
            placeholder: 'Chọn một',
            allowClear: true
        });
    } else {
        console.error("Select2 is not loaded");
    }

    // Kiểm tra và khởi tạo Summernote
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

    // Hàm định dạng giá
    function formatPrice(input) {
        let value = input.value.replace(/\D/g, '');
        if (value === '') return;
        value = parseInt(value, 10);
        input.value = value.toLocaleString('vi-VN');
    }

    $('.price-input').on('input', function () {
        formatPrice(this);
    });

    // Hàm đọc và hiển thị ảnh
    function readURL(input) {
        if (input.files && input.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var container = $(input).closest('.image-upload');
                container.find('img').remove();
                container.find('.remove-image').remove();
                container.find('label').hide();
                $('<img>').attr({
                    'src': e.target.result,
                    'alt': 'Ảnh đã chọn'
                }).appendTo(container);
                $('<span class="remove-image">&times;</span>').appendTo(container).show();
            }
            reader.readAsDataURL(input.files[0]);
        }
    }

    // Hàm reset ô upload ảnh
    function resetImageUpload(container) {
        var index = container.data('index');
        container.html(`
            <label for="AdditionalImage${index}">
                <i class="fas fa-plus"></i> Chọn ảnh
            </label>
            <input type="file" id="AdditionalImage${index}" name="AdditionalImages" accept="image/*" style="display: none;">
        `);
    }

    // Xử lý ảnh chính
    $("#MainImageFile").change(function () {
        readURL(this);
    });

    $("#mainImageUpload").on("click", ".remove-image", function (e) {
        e.preventDefault();
        var container = $(this).closest('.image-upload');
        container.find('input[type="file"]').val('');
        container.find('img').remove();
        container.find('.remove-image').remove();
        container.find('label').show();
        $('<input>').attr({
            type: 'hidden',
            name: 'DeleteMainImage',
            value: 'true'
        }).appendTo('#editRoomForm');
    });

    // Xử lý ảnh bổ sung
    $("#additionalImagesContainer").on("change", "input[type='file']", function () {
        readURL(this);
    });

    $("#additionalImagesContainer").on("click", ".remove-image", function (e) {
        e.preventDefault();
        var container = $(this).closest('.image-upload');
        var imageId = container.find('input[name="ExistingImageIds"]').val();
        if (imageId) {
            $('<input>').attr({
                type: 'hidden',
                name: 'DeletedImageIds',
                value: imageId
            }).appendTo('#editRoomForm');
        }
        resetImageUpload(container);
    });

    function resetImageUpload(container) {
        var index = container.data('index');
        container.html(`
        <label for="AdditionalImage${index}">
            <i class="fas fa-plus"></i> Chọn ảnh
        </label>
        <input type="file" id="AdditionalImage${index}" name="AdditionalImages" accept="image/*" style="display: none;">
    `);
    }

    // Xử lý submit form
    $("#editRoomForm").submit(function (e) {
        e.preventDefault();
        var formData = new FormData(this);

        var priceValue = $('.price-input').val().replace(/\D/g, '');
        formData.set('PriceRoom', priceValue);

        // Kiểm tra xem có hình chính không
        var mainImage = $('#MainImageFile')[0].files[0];
        var existingMainImage = $('#mainImageUpload img').length > 0;
        if (!mainImage && !existingMainImage) {
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
                        text: result.message || 'Có lỗi xảy ra khi cập nhật phòng. Vui lòng thử lại.',
                        confirmButtonText: 'OK'
                    });
                }
            },
            error: function (xhr, status, error) {
                if (xhr.status === 401) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Phiên đăng nhập đã hết hạn',
                        text: 'Vui lòng đăng nhập lại để tiếp tục.',
                        confirmButtonText: 'Đăng nhập'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            window.location.href = '/' + collaboratorCode + '/manager/rooms';
                        }
                    });
                } else {
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

    // Định dạng giá ban đầu
    formatPrice($('.price-input')[0]);

    // Hiển thị nút xóa cho ảnh hiện có
    $('.image-upload').each(function () {
        if ($(this).find('img').attr('src') !== '#') {
            $(this).find('.remove-image').show();
        }
    });

    console.log("Script loaded successfully");
    $(".image-upload input[type='file']").each(function () {
        console.log("File input found:", this.id);
    });

    $('.image-upload').each(function () {
        if ($(this).find('img').length > 0) {
            $(this).find('label').hide();
            $(this).find('.remove-image').show();
        }
    });
});