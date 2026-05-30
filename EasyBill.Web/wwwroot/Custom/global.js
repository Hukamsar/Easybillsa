$(document).ready(function () {
   // $(".btn-delete").on("click", function (e) {
    $(document).on("click", ".btn-delete", function (e) {
        e.preventDefault();
        var id = $(this).data("id");
        var controller = $(this).data("controller");
        var row = $(this).closest("tr");

        Swal.fire({
            title: 'Are you sure?',
            text: "You won't be able to revert this!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, delete it!'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: `/${controller}/Delete`,
                    type: 'POST',
                    data: { id: id },
                    success: function (response) {
                        if (response.success) {
                            Swal.fire(
                                'Deleted!',
                                response.message,
                                'success'
                            );
                            // row.remove();
                            var table = row.closest("table").DataTable();
                            table.row(row).remove().draw(false);
                            if ($("tbody tr").length === 0) {
                                $("tbody").html('<tr><td colspan="13">No data available.</td></tr>');
                            }
                        } else {
                            Swal.fire(
                                'Error!',
                                'Can not delete.This is used in other process!',
                                'error'
                            );
                        }
                    },
                    error: function () {
                        Swal.fire(
                            'Error!',
                            'An error occurred while deleting the record.',
                            'error'
                        );
                    }
                });
            }
        });
    });
});

function confirmSave(callback) {
    Swal.fire({
        title: 'Are you sure?',
        text: 'Do you want to save this record?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#198754', // green
        cancelButtonColor: '#d33',     // red
        confirmButtonText: 'Yes, Save',
        cancelButtonText: 'No'
    }).then((result) => {
        if (result.isConfirmed) {
            callback(true);   // ✅ YES
        } else {
            callback(false);  // ❌ NO
        }
    });
}

