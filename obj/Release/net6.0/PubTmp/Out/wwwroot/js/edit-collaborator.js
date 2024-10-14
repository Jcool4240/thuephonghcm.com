$(document).ready(function () {
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

    // Xử lý ảnh đại diện
    $("#AvatarFile").change(function () {
        readURL(this);
    });

    $("#avatarUpload").on("click", ".remove-image", function (e) {
        e.preventDefault();
        var container = $(this).closest('.image-upload');
        container.find('input[type="file"]').val('');
        container.find('img').remove();
        container.find('.remove-image').remove();
        container.find('label').show();
        $('<input>').attr({
            type: 'hidden',
            name: 'DeleteAvatar',
            value: 'true'
        }).appendTo('#editCollaboratorForm');
    });

    // Xử lý ảnh công việc
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
            }).appendTo('#editCollaboratorForm');
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
    $("#editCollaboratorForm").submit(function (e) {
        e.preventDefault();
        var formData = new FormData(this);

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
                            window.location.href = '/' + collaboratorCode + '/manager/about';
                        }
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi!',
                        text: result.message || 'Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại.',
                        confirmButtonText: 'OK'
                    });
                }
            },
            error: function (xhr, status, error) {
                console.error("Error:", xhr.responseText);
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi!',
                    text: 'Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại.',
                    confirmButtonText: 'OK'
                });
            },
            cache: false,
            contentType: false,
            processData: false
        });
    });
});