(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", async function () {
        const infoEl = document.getElementById("walletInfo");
        const historyBodyEl = document.getElementById("walletHistoryBody");
        const amountEl = document.getElementById("walletRechargeAmount");
        const remarksEl = document.getElementById("walletRechargeRemarks");
        const modeEl = document.getElementById("walletRechargeMode");
        const quickAmountBtns = document.querySelectorAll(".wallet-quick-amount");
        const manualSectionEl = document.getElementById("walletManualSection");
        const manualRefEl = document.getElementById("walletManualReferenceNo");
        const payBtn = document.getElementById("btnWalletProceedRecharge");

        const saveSettingsBtn = document.getElementById("btnSaveWalletSettings");
        const smsChargeEl = document.getElementById("smsCharge");
        const emailChargeEl = document.getElementById("emailCharge");
        const whatsappChargeEl = document.getElementById("whatsappCharge");
        const smsChargeActiveEl = document.getElementById("smsChargeActive");
        const emailChargeActiveEl = document.getElementById("emailChargeActive");
        const whatsappChargeActiveEl = document.getElementById("whatsappChargeActive");
        const walletActiveEl = document.getElementById("walletActive");

        let walletData = null;

        const notify = function (type, message) {
            if (window.toastr) {
                if (type === "success") toastr.success(message, "Wallet");
                else if (type === "info") toastr.info(message, "Wallet");
                else toastr.error(message, "Wallet");
                return;
            }
            alert(message);
        };

        const getJson = async function (url) {
            try {
                const response = await fetch(url, { credentials: "same-origin" });
                return await response.json();
            } catch {
                return { success: false, message: "Network error." };
            }
        };

        const postJson = async function (url, payload) {
            try {
                const response = await fetch(url, {
                    method: "POST",
                    credentials: "same-origin",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload || {})
                });
                return await response.json();
            } catch {
                return { success: false, message: "Network error." };
            }
        };

        const toMoney = function (value) {
            return Number(value || 0).toFixed(2);
        };

        const toggleManualSection = function () {
            const isManual = modeEl.value === "manual";
            manualSectionEl.classList.toggle("d-none", !isManual);
            manualRefEl.required = isManual;
        };

        const getServiceCharge = function () {
            const whatsapp = Number(walletData?.whatsAppMessageCharge || 0);
            return Number(whatsapp.toFixed(2));
        };

        const loadWalletData = async function () {
            const result = await getJson("/Wallet/GetWallet");
            if (!result?.success) {
                notify("error", result?.message || "Wallet load failed.");
                return;
            }

            walletData = result.data || {};
            const walletStatus = walletData.isWalletActive ? "Active" : "Inactive";
            infoEl.innerText = `${walletData.tenantName || "-"} | Balance: Rs.${toMoney(walletData.walletBalance)} | Wallet: ${walletStatus}`;

            smsChargeEl.value = toMoney(walletData.smsMessageCharge);
            emailChargeEl.value = toMoney(walletData.emailMessageCharge);
            whatsappChargeEl.value = toMoney(walletData.whatsAppMessageCharge);
            smsChargeActiveEl.checked = walletData.isSmsChargeActive === true;
            emailChargeActiveEl.checked = walletData.isEmailChargeActive === true;
            whatsappChargeActiveEl.checked = walletData.isWhatsAppChargeActive === true;
            walletActiveEl.checked = walletData.isWalletActive !== false;

            const history = walletData.rechargeHistory || [];
            if (!history.length) {
                historyBodyEl.innerHTML = '<tr><td colspan="5" class="text-center">No recharge history.</td></tr>';
                return;
            }

            historyBodyEl.innerHTML = history.map(function (x) {
                const dt = x.transactionDateTime ? new Date(x.transactionDateTime).toLocaleString() : "-";
                const amountValue = Number(x.amount || 0);
                const sign = amountValue < 0 ? "-" : "";
                const amt = `${sign}Rs.${toMoney(Math.abs(amountValue))}`;
                const amountClass = x.isDebit || amountValue < 0 ? "text-danger fw-semibold" : "text-success";
                return `<tr>
                    <td>${dt}</td>
                    <td class="${amountClass}">${amt}</td>
                    <td>${x.paymentMode || "-"}</td>
                    <td>${x.referenceNo || "-"}</td>
                    <td>${x.note || "-"}</td>
                </tr>`;
            }).join("");
        };

        const saveSettings = async function () {
            const payload = {
                smsMessageCharge: Number(smsChargeEl.value || 0),
                emailMessageCharge: Number(emailChargeEl.value || 0),
                whatsAppMessageCharge: Number(whatsappChargeEl.value || 0),
                isSmsChargeActive: !!smsChargeActiveEl.checked,
                isEmailChargeActive: !!emailChargeActiveEl.checked,
                isWhatsAppChargeActive: !!whatsappChargeActiveEl.checked,
                isWalletActive: !!walletActiveEl.checked
            };

            const result = await postJson("/Wallet/SaveSettings", payload);
            if (!result?.success) {
                notify("error", result?.message || "Unable to save settings.");
                return;
            }

            notify("success", result?.message || "Settings saved.");
            await loadWalletData();
        };

        const handleRecharge = async function () {
            const amount = Number(amountEl.value || 0);
            if (amount <= 0) {
                notify("error", "Please enter a valid amount.");
                return;
            }

            if (!walletData?.isWalletActive) {
                notify("error", "Wallet inactive hai. Pehle wallet active karein.");
                return;
            }

            const mode = modeEl.value;
            const serviceType = "whatsapp";
            const serviceCharge = getServiceCharge();
            const remarks = (remarksEl.value || "").trim();

            if (mode === "manual") {
                const referenceNo = (manualRefEl.value || "").trim();
                if (!referenceNo) {
                    notify("error", "Manual mode me reference no required hai.");
                    return;
                }

                const manualResult = await postJson("/Wallet/AdminRecharge", {
                    amount: amount,
                    remarks: remarks,
                    referenceNo: referenceNo,
                    serviceType: serviceType,
                    serviceCharge: serviceCharge
                });

                if (!manualResult?.success) {
                    notify("error", manualResult?.message || "Manual recharge failed.");
                    return;
                }

                notify("success", manualResult?.message || "Manual recharge successful.");
                amountEl.value = "";
                manualRefEl.value = "";
                remarksEl.value = "";
                await loadWalletData();
                return;
            }

            const orderResult = await postJson("/Wallet/CreateGatewayOrder", {
                amount: amount,
                remarks: remarks,
                serviceType: serviceType,
                serviceCharge: serviceCharge
            });

            if (!orderResult?.success) {
                const detailText = orderResult?.details ? ` ${orderResult.details}` : "";
                notify("error", `${orderResult?.message || "Unable to create order."}${detailText}`);
                return;
            }

            const order = orderResult.data || {};
            const rzp = new Razorpay({
                key: order.key,
                amount: order.amountInPaise,
                currency: order.currency || "INR",
                name: order.tenantName || "EasyBill",
                description: "Wallet Recharge",
                order_id: order.orderId,
                prefill: {
                    name: order.tenantName || "",
                    email: order.tenantEmail || "",
                    contact: order.tenantMobileNo || ""
                },
                handler: async function (response) {
                    const verify = await postJson("/Wallet/ConfirmGatewayRecharge", {
                        gatewayOrderId: response.razorpay_order_id,
                        gatewayTransactionId: response.razorpay_payment_id,
                        gatewaySignature: response.razorpay_signature
                    });

                    if (!verify?.success) {
                        notify("error", verify?.message || "Payment verification failed.");
                        return;
                    }

                    notify("success", verify?.message || "Recharge successful.");
                    amountEl.value = "";
                    remarksEl.value = "";
                    await loadWalletData();
                },
                modal: {
                    ondismiss: function () {
                        notify("info", "Payment cancelled.");
                    }
                },
                theme: { color: "#2b6ac9" }
            });

            rzp.on("payment.failed", function (response) {
                notify("error", response?.error?.description || "Payment failed.");
            });

            rzp.open();
        };

        await loadWalletData();
        toggleManualSection();

        modeEl.addEventListener("change", toggleManualSection);
        saveSettingsBtn.addEventListener("click", saveSettings);
        quickAmountBtns.forEach(function (btn) {
            btn.addEventListener("click", function () {
                const amount = Number(btn.dataset.amount || 0);
                if (amount > 0) {
                    amountEl.value = amount.toFixed(2);
                }
            });
        });

        payBtn.addEventListener("click", async function () {
            try {
                payBtn.disabled = true;
                await handleRecharge();
            } catch (e) {
                notify("error", e?.message || "Recharge failed.");
            } finally {
                payBtn.disabled = false;
            }
        });
    });
})();
