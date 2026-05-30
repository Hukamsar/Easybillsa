(function () {
    if (window.easyBillPaymentModePopupLoaded) return;

    window.easyBillPaymentModePopupLoaded = true;
    window.paymentModeActiveDropdown = null;
    window.easyBillPaymentModes = [];

    const urls = {
        list: "/PaymentMode/GetPaymentModes",
        get: "/PaymentMode/GetPaymentMode",
        save: "/PaymentMode/PaymentModeSave"
    };

    function msg(type, message) {
        window.toastr ? toastr[type](message) : alert(message);
    }

    function modalHtml() {
        return `<div class="modal fade" id="paymentModeModal" tabindex="-1">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content custom-modal">
                    <div class="modal-header">
                        <h5 class="modal-title" id="paymentModeModalTitle">Payment Mode</h5>
                    </div>
                    <div class="modal-body">
                        <input type="hidden" id="paymentModeId" value="0" />

                        <label>Name <span class="text-danger">*</span></label>
                        <input type="text" id="paymentModeName" class="modal-input mb-2" autocomplete="off" />

                        <label>Payment Type <span class="text-danger">*</span></label>
                        <select id="paymentModeType" class="modal-input mb-2">
                            <option value="">Select Payment Type</option>
                            <option value="1">Cash</option>
                            <option value="2">Bank</option>
                        </select>

                        <label>Description</label>
                        <input type="text" id="paymentModeDescription" class="modal-input" autocomplete="off" />

                        <div id="paymentModeError" class="text-danger small modal-error"></div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-sm btn-success" id="btnSavePaymentModePopup">Save</button>
                        <button type="button" class="btn btn-sm btn-danger" data-bs-dismiss="modal">Cancel</button>
                    </div>
                </div>
            </div>
        </div>`;
    }

    function ensureModal() {
        if (!$("#paymentModeModal").length) {
            $("body").append(modalHtml());
        }
    }

    function resetModal(title) {
        $("#paymentModeId").val("0");
        $("#paymentModeName").val("");
        $("#paymentModeType").val("");
        $("#paymentModeDescription").val("");
        $("#paymentModeError").text("");
        $("#paymentModeModalTitle").text(title);
    }

    function showModal() {
        $("#paymentModeModal").modal("show");
        setTimeout(function () {
            $("#paymentModeName").focus();
        }, 300);
    }

    function addPaymentModeDropDownLinks() {
        $("th .payment-mode-shortcut-box").closest("th").text("Payment Mode");

        $(".paymentMode-dropdown").each(function () {
            const dropdown = $(this);

            if (dropdown.closest(".payment-mode-dropdown-wrap").length) return;

            dropdown.addClass("flex-grow-1");
            dropdown.wrap('<div class="d-flex align-items-center payment-mode-dropdown-wrap"></div>');

            dropdown.after(`<div class="shortcut-box payment-mode-shortcut-box">
                <a href="javascript:void(0)" class="shortcut-link d-block small btnPaymentModeNew">F2 - New</a>
                <a href="javascript:void(0)" class="shortcut-link text-danger d-block small btnPaymentModeMod">F3 - Mod</a>
            </div>`);
        });
    }

    function setPaymentModeFunctions() {
        const optionHtml = window.easyBillPaymentModes
            .map(x => `<option value="${x.id}">${x.name}</option>`)
            .join("");

        window.getPaymentOptions = function () {
            return `<option value="">Select</option>${optionHtml}`;
        };

        window.renderPaymentOptions = function (selectedValue) {
            let html = '<option value="">-- Select --</option>';

            window.easyBillPaymentModes.forEach(function (item) {
                const selected = String(selectedValue || "") === String(item.id) ? "selected" : "";
                html += `<option value="${item.id}" ${selected}>${item.name}</option>`;
            });

            return html;
        };
    }

    function refreshPaymentModeDropDowns(selectedId) {
        const oldValues = $(".paymentMode-dropdown").map(function () {
            return $(this).val();
        }).get();

        $.get(urls.list, function (res) {
            if (!res || !res.success) return;

            window.easyBillPaymentModes = res.data || [];
            setPaymentModeFunctions();

            $(".paymentMode-dropdown").each(function (index) {
                const dropdown = $(this);
                const oldValue =
                    dropdown[0] === window.paymentModeActiveDropdown && selectedId
                        ? selectedId
                        : oldValues[index];

                dropdown.empty().append('<option value="">Select</option>');

                window.easyBillPaymentModes.forEach(function (item) {
                    dropdown.append($("<option></option>").val(item.id).text(item.name));
                });

                if (oldValue) dropdown.val(oldValue);
            });

            addPaymentModeDropDownLinks();
        });
    }

    function openPaymentModeNew() {
        ensureModal();
        resetModal("Create Payment Mode");
        showModal();
    }

    function getSelectedPaymentModeId() {
        const active = $(window.paymentModeActiveDropdown);
        let id = active.length ? active.val() : "";

        if (!id) {
            id = $(".paymentMode-dropdown")
                .filter(function () { return $(this).val(); })
                .first()
                .val();
        }

        return id;
    }

    function openPaymentModeMod() {
        const id = getSelectedPaymentModeId();

        if (!id) {
            msg("error", "Please select payment mode.");
            return;
        }

        ensureModal();
        $("#paymentModeError").text("");

        $.get(urls.get, { id: id }, function (res) {
            if (!res || !res.success) {
                msg("error", res && res.message ? res.message : "Payment mode not found.");
                return;
            }

            $("#paymentModeId").val(res.data.id);
            $("#paymentModeName").val(res.data.name || "");
            $("#paymentModeType").val(res.data.paymentType || "");
            $("#paymentModeDescription").val(res.data.description || "");
            $("#paymentModeModalTitle").text("Modify Payment Mode");

            $("#paymentModeModal").modal("show");

            setTimeout(function () {
                $("#paymentModeName").focus().select();
            }, 300);
        });
    }

    function savePaymentMode() {
        const data = {
            Id: $("#paymentModeId").val() || 0,
            Name: $.trim($("#paymentModeName").val()),
            PaymentType: $("#paymentModeType").val(),
            Description: $("#paymentModeDescription").val()
        };

        $("#paymentModeError").text("");

        if (!data.Name) {
            $("#paymentModeError").text("Please enter Name.");
            $("#paymentModeName").focus();
            return;
        }

        if (!data.PaymentType) {
            $("#paymentModeError").text("Please select Payment Type.");
            $("#paymentModeType").focus();
            return;
        }

        $.ajax({
            url: urls.save,
            type: "POST",
            data: data,
            success: function (res) {
                if (!res || !res.success) {
                    $("#paymentModeError").text(res && res.message ? res.message : "Payment mode save failed.");
                    return;
                }

                $("#paymentModeModal").modal("hide");
                refreshPaymentModeDropDowns(res.data.id);
                msg("success", res.message || "Payment mode saved.");
            },
            error: function () {
                $("#paymentModeError").text("Payment mode save failed.");
            }
        });
    }

    function setActiveDropdownFromButton(btn) {
        const dropdown = $(btn)
            .closest(".payment-mode-dropdown-wrap")
            .find(".paymentMode-dropdown");

        if (dropdown.length) {
            window.paymentModeActiveDropdown = dropdown[0];
        }
    }

    $(document).on("focus click", ".paymentMode-dropdown", function () {
        window.paymentModeActiveDropdown = this;
    });

    $(document).on("click", ".btnPaymentModeNew", function () {
        setActiveDropdownFromButton(this);
        openPaymentModeNew();
    });

    $(document).on("click", ".btnPaymentModeMod", function () {
        setActiveDropdownFromButton(this);
        openPaymentModeMod();
    });

    $(document).on("keydown", ".paymentMode-dropdown", function (e) {
        if (e.key === "F2" || e.key === "F3") {
            e.preventDefault();
            window.paymentModeActiveDropdown = this;

            e.key === "F2" ? openPaymentModeNew() : openPaymentModeMod();
        }
    });

    $(document).on("click", "#btnSavePaymentModePopup", savePaymentMode);

    $(document).on("keypress", "#paymentModeName,#paymentModeType,#paymentModeDescription", function (e) {
        if (e.which === 13) {
            e.preventDefault();
            savePaymentMode();
        }
    });

    $(document).on("click", "#addPaymentRow", function () {
        setTimeout(function () {
            refreshPaymentModeDropDowns();
            addPaymentModeDropDownLinks();
        }, 200);
    });

    $(function () {
        ensureModal();
        addPaymentModeDropDownLinks();

        const body = document.getElementById("PaymentDetailsBody");

        if (body) {
            new MutationObserver(addPaymentModeDropDownLinks)
                .observe(body, { childList: true, subtree: true });
        }
    });

})();