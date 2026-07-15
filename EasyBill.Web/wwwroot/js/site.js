// Global AJAX Setup for JWT Tokens
$.ajaxSetup({
    beforeSend: function (xhr) {
        xhr.setRequestHeader("Authorization", "Bearer " + localStorage.getItem("jwtToken"));
    },
    complete: function (xhr) {
        var newToken = xhr.getResponseHeader("X-New-JWT-Token");
        if (newToken) {
            localStorage.setItem("jwtToken", newToken);
        }
    }
}); 

/*
// Helper to serialize data to a string (handles plain objects, FormData, and strings)
function serializeData(data) {
    if (!data) return "";
    if (typeof data === "string") return data;
    if (typeof FormData !== "undefined" && data instanceof FormData) {
        const obj = {};
        data.forEach((value, key) => {
            if (typeof File !== "undefined" && value instanceof File) {
                obj[key] = "[File: " + value.name + "]";
            } else {
                obj[key] = value;
            }
        });
        return JSON.stringify(obj);
    }
    if (typeof data === "object") {
        try {
            return $.param(data);
        } catch (e) {
            console.warn("Failed to serialize data with $.param:", e);
            try {
                return JSON.stringify(data);
            } catch (je) {
                return "";
            }
        }
    }
    return data.toString();
}

// Save AJAX POST request offline
async function saveAjaxOffline(url, data, contentType) {
    try {
        if (typeof hideLoader === 'function') {
            hideLoader();
        }
        const serialized = serializeData(data);
        
        const formObj = {
            action: url,
            method: "POST",
            timestamp: Date.now(),
            isAjax: true,
            contentType: contentType || "application/x-www-form-urlencoded; charset=UTF-8",
            fields: serialized
        };

        const db = await getOfflineDB();
        const tx = db.transaction(OFFLINE_STORE_NAME, "readwrite");
        tx.objectStore(OFFLINE_STORE_NAME).add(formObj);
        
        await new Promise((resolve, reject) => {
            tx.oncomplete = resolve;
            tx.onerror = reject;
        });

        // Notify user
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'warning',
                title: 'Working Offline',
                text: 'Connection to server failed. Your transaction has been saved locally and will sync automatically when the connection is restored.',
                confirmButtonColor: '#1d4ed8'
            });
        } else {
            alert('Working Offline: Transaction saved locally. It will sync automatically when online.');
        }

        // If there is an active sales form on the page, reset it so they can do another transaction
        if (typeof resetSalesForm === 'function') {
            resetSalesForm();
        } else {
            const activeForm = document.getElementById("salesForm") || document.querySelector('form');
            if (activeForm) activeForm.reset();
        }
    } catch (err) {
        console.error("Failed to save offline AJAX transaction:", err);
        alert("Error saving transaction offline. Please check connection.");
    }
}

// Prefilter to capture and intercept network failures on POST AJAX requests
$.ajaxPrefilter(function (options, originalOptions, jqXHR) {
    if (options.type.toUpperCase() !== "POST") return;
    
    // Ignore internal calls, polls, logins, or sync endpoints
    const urlLower = options.url.toLowerCase();
    if (urlLower.includes("sync") || urlLower.includes("health") || urlLower.includes("login")) {
        return;
    }

    // Set default timeout of 5 seconds to prevent hanging in dead-zone Wi-Fi (Lie-Fi)
    if (!options.timeout) {
        options.timeout = 5000;
    }

    // If we already know the server is unreachable, intercept immediately
    if (!isServerReachable) {
        setTimeout(() => {
            console.warn("Server known to be unreachable. Intercepting AJAX POST immediately:", options.url);
            saveAjaxOffline(options.url, options.data, options.contentType);
            jqXHR.abort();
        }, 0);
        return;
    }

    const originalError = options.error;
    options.error = function (xhr, status, error) {
        if (xhr.status === 0 || xhr.status === 503 || status === "timeout" || error === "timeout") {
            console.warn("Detected offline/unreachable network during POST. Saving offline:", options.url);
            saveAjaxOffline(options.url, options.data, options.contentType);
            return; // Prevent original error handler (like toastr) from showing misleading errors
        }
        if (originalError) {
            originalError.call(this, xhr, status, error);
        }
    };
});

// ============================================================================
// OFFLINE FORM DB & AUTO-SYNC ENGINE
// ============================================================================
const OFFLINE_DB_NAME = "EasyBillOfflineDB";
const OFFLINE_STORE_NAME = "offlineForms";

function getOfflineDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(OFFLINE_DB_NAME, 1);
        request.onupgradeneeded = e => {
            const db = e.target.result;
            if (!db.objectStoreNames.contains(OFFLINE_STORE_NAME)) {
                db.createObjectStore(OFFLINE_STORE_NAME, { keyPath: "id", autoIncrement: true });
            }
        };
        request.onsuccess = e => resolve(e.target.result);
        request.onerror = e => reject(e.target.error);
    });
}

async function saveFormOffline(form) {
    try {
        if (typeof hideLoader === 'function') {
            hideLoader();
        }
        const formData = new FormData(form);
        const action = form.getAttribute('action') || window.location.pathname;
        const formObj = {
            action: action,
            method: form.method || "POST",
            timestamp: Date.now(),
            fields: {}
        };

        // Serialize all form fields
        formData.forEach((value, key) => {
            formObj.fields[key] = value;
        });

        const db = await getOfflineDB();
        const tx = db.transaction(OFFLINE_STORE_NAME, "readwrite");
        tx.objectStore(OFFLINE_STORE_NAME).add(formObj);
        
        await new Promise((resolve, reject) => {
            tx.oncomplete = resolve;
            tx.onerror = reject;
        });

        // Notify user and stay on page (reset form to allow more entries offline)
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'warning',
                title: 'Working Offline',
                text: 'Connection to server failed. Your data has been saved locally and will sync automatically when the connection is restored.',
                confirmButtonColor: '#1d4ed8'
            }).then(() => {
                form.reset();
            });
        } else {
            alert('Working Offline: Data saved locally. It will sync automatically when online.');
            form.reset();
        }
    } catch (err) {
        console.error("Failed to save offline form:", err);
        alert("Error saving data offline. Please check connection.");
    }
}

async function syncOfflineData() {
    if (!navigator.onLine) return;

    try {
        const db = await getOfflineDB();
        const tx = db.transaction(OFFLINE_STORE_NAME, "readonly");
        const store = tx.objectStore(OFFLINE_STORE_NAME);
        const request = store.getAll();

        request.onsuccess = async () => {
            const forms = request.result;
            if (!forms || forms.length === 0) return;

            console.log(`Syncing ${forms.length} offline forms...`);
            if (typeof toastr !== 'undefined') {
                toastr.info(`Syncing ${forms.length} offline actions...`);
            }

            let successCount = 0;
            let failureCount = 0;
            const errorMessages = [];

            for (const formObj of forms) {
                let body;
                const headers = {};
                const jwtToken = localStorage.getItem("jwtToken");
                if (jwtToken) {
                    headers["Authorization"] = "Bearer " + jwtToken;
                }

                // Make URL relative to the current origin to avoid host/port mismatch on other systems
                let relativeAction = formObj.action;
                try {
                    const actionUrl = new URL(formObj.action, window.location.origin);
                    relativeAction = actionUrl.pathname + actionUrl.search;
                } catch (urlErr) {
                    console.warn("Could not parse action URL, using original:", formObj.action);
                }

                if (formObj.isAjax) {
                    // Check if fields is a JSON string representing serialized FormData or object
                    let isJsonString = false;
                    let parsedFields = null;
                    if (typeof formObj.fields === "string" && (formObj.fields.startsWith("{") || formObj.fields.startsWith("["))) {
                        try {
                            parsedFields = JSON.parse(formObj.fields);
                            isJsonString = true;
                        } catch (e) {}
                    }

                    if (isJsonString && parsedFields) {
                        if (formObj.contentType && formObj.contentType.includes("json")) {
                            body = JSON.stringify(parsedFields);
                            headers["Content-Type"] = formObj.contentType;
                        } else if (formObj.contentType && formObj.contentType.includes("multipart/form-data")) {
                            const formData = new FormData();
                            for (const [key, val] of Object.entries(parsedFields)) {
                                formData.append(key, val);
                            }
                            body = formData;
                            // Content-Type is intentionally omitted for multipart/form-data
                        } else {
                            body = $.param(parsedFields);
                            headers["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8";
                        }
                    } else {
                        body = formObj.fields;
                        if (formObj.contentType) {
                            headers["Content-Type"] = formObj.contentType;
                        } else {
                            headers["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8";
                        }
                    }
                } else {
                    // Standard Form Post: Send as application/x-www-form-urlencoded to match original browser submit
                    body = new URLSearchParams();
                    for (const [key, value] of Object.entries(formObj.fields)) {
                        body.append(key, value);
                    }
                    headers["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8";
                }

                try {
                    const response = await fetch(relativeAction, {
                        method: formObj.method,
                        headers: headers,
                        body: body,
                        credentials: "include" // Ensure session cookies are sent!
                    });

                    let syncSuccess = false;
                    let errorDetails = "";

                    if (response.ok) {
                        const responseText = await response.text();
                        
                        // Check if redirected to login page
                        if (response.url && response.url.toLowerCase().includes("/login")) {
                            syncSuccess = false;
                            errorDetails = "Session expired. Please log in again.";
                            console.warn(`Sync failed: Redirected to login page for ${relativeAction}`);
                        }
                        // Check for common ASP.NET Core validation error markers in HTML
                        else if (responseText.includes("validation-summary-errors") || 
                                 responseText.includes("field-validation-error") || 
                                 responseText.includes("text-danger") ||
                                 responseText.includes("toastr.error")) {
                            syncSuccess = false;
                            
                            // Try to extract the error message from the HTML
                            try {
                                const parser = new DOMParser();
                                const doc = parser.parseFromString(responseText, "text/html");
                                
                                // Try to find field-validation-error or validation-summary-errors
                                const valError = doc.querySelector(".field-validation-error, .validation-summary-errors");
                                if (valError) {
                                    errorDetails = valError.textContent.trim();
                                }
                                
                                // Try to find toastr.error in scripts
                                if (!errorDetails) {
                                    const scripts = doc.querySelectorAll("script");
                                    for (const script of scripts) {
                                        const content = script.textContent;
                                        if (content && content.includes("toastr.error")) {
                                            const match = content.match(/toastr\.error\(['"](.*)['"]\)/);
                                            if (match && match[1]) {
                                                errorDetails = match[1];
                                                break;
                                            }
                                        }
                                    }
                                }
                            } catch (parseErr) {}

                            if (!errorDetails) {
                                errorDetails = "Validation failed on server.";
                            }
                            console.warn(`Sync failed for ${relativeAction}: ${errorDetails}`);
                        }
                        else {
                            try {
                                const result = JSON.parse(responseText);
                                if (result && result.success !== undefined) {
                                    syncSuccess = result.success;
                                    if (!syncSuccess && result.message) {
                                        errorDetails = result.message;
                                    }
                                } else {
                                    syncSuccess = true;
                                }
                            } catch (jsonErr) {
                                // If it's HTML without validation errors/login, assume success
                                syncSuccess = true;
                            }
                        }
                    } else {
                        syncSuccess = false;
                        errorDetails = `Server returned status ${response.status} (${response.statusText}).`;
                    }

                    if (syncSuccess) {
                        const deleteTx = db.transaction(OFFLINE_STORE_NAME, "readwrite");
                        deleteTx.objectStore(OFFLINE_STORE_NAME).delete(formObj.id);
                        console.log(`Successfully synced offline action: ${relativeAction}`);
                        successCount++;
                    } else {
                        console.warn(`Sync failed or rejected for action: ${relativeAction}`);
                        failureCount++;
                        if (errorDetails) {
                            errorMessages.push(errorDetails);
                        }
                        
                        // Discard permanent client errors (4xx codes except 401/403 session expiration, 408 timeout, and 429 rate limit)
                        if (response.status >= 400 && response.status < 500 &&
                            response.status !== 401 && response.status !== 403 &&
                            response.status !== 408 && response.status !== 429) {
                            try {
                                const deleteTx = db.transaction(OFFLINE_STORE_NAME, "readwrite");
                                deleteTx.objectStore(OFFLINE_STORE_NAME).delete(formObj.id);
                                console.warn(`Permanently failed offline action (Status ${response.status}) discarded: ${relativeAction}`);
                            } catch (delErr) {
                                console.error("Failed to delete permanently failed action from IndexedDB:", delErr);
                            }
                        }
                    }
                } catch (fetchErr) {
                    console.error("Failed to send sync request:", fetchErr);
                    failureCount++;
                    errorMessages.push("Network/connection timeout.");
                    break; // Stop syncing loop if network request fails (still offline)
                }
            }

            if (typeof toastr !== 'undefined') {
                if (successCount > 0 && failureCount === 0) {
                    toastr.success("Offline data synced successfully!");
                    setTimeout(() => {
                        window.location.reload();
                    }, 1500);
                } else if (successCount > 0 && failureCount > 0) {
                    const uniqueErrors = [...new Set(errorMessages)].join(", ");
                    toastr.warning(`Synced ${successCount} items. ${failureCount} items failed to sync: ${uniqueErrors}`);
                } else if (failureCount > 0) {
                    const uniqueErrors = [...new Set(errorMessages)].join(", ");
                    toastr.error(`Offline sync failed: ${uniqueErrors || "Please log in or verify input."}`);
                }
            }
        };
    } catch (err) {
        console.error("Sync error:", err);
    }
}

// Track actual server reachability in the background (prevents submit lag and handles Wi-Fi-no-internet cases)
let isServerReachable = navigator.onLine;

async function checkServerStatus() {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 1500); // 1.5 second timeout

    try {
        const response = await fetch("/health", { 
            method: "GET", 
            cache: "no-store",
            signal: controller.signal
        });
        clearTimeout(timeoutId);
        isServerReachable = response.ok;
        console.log("Server reachability checked. Reachable:", isServerReachable);
    } catch (e) {
        clearTimeout(timeoutId);
        isServerReachable = false;
        console.warn("Server reachability check failed (offline/timeout):", e);
    }
}

// Periodically poll server status every 10 seconds
setInterval(checkServerStatus, 10000);
checkServerStatus();

// Handle connection event listeners
window.addEventListener('online', () => { 
    console.log("Browser fired online event. Verifying server status...");
    checkServerStatus(); 
});
window.addEventListener('offline', () => { 
    console.log("Browser fired offline event. Setting reachability to false.");
    isServerReachable = false; 
});

// Check if we were redirected after an offline form save in the Service Worker
if (localStorage.getItem('offline_save_detected') === 'true') {
    localStorage.removeItem('offline_save_detected');
    setTimeout(() => {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'warning',
                title: 'Working Offline',
                text: 'Connection to server failed. Your data has been saved locally and will sync automatically when the connection is restored.',
                confirmButtonColor: '#1d4ed8'
            });
        } else {
            alert('Working Offline: Data saved locally. It will sync automatically when online.');
        }
    }, 500);
}

// Trigger dynamic pre-caching of authenticated pages in service worker
function triggerPageCaching() {
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        if (localStorage.getItem("jwtToken") || !window.location.pathname.toLowerCase().includes("login")) {
            console.log("Requesting Service Worker to cache authenticated pages...");
            navigator.serviceWorker.controller.postMessage({
                action: 'cachePages',
                pages: ['/Sales/Create', '/CategoryMaster/Create', '/ItemMaster/Create']
            });
        }
    }
}

function initOfflineEngine() {
    // 1. Intercept form submits when offline
    document.addEventListener('submit', function (event) {
        const form = event.target;
        const method = (form.method || "GET").toUpperCase();
        if (method !== 'POST') return;

        // If the submit event was already defaultPrevented (e.g. handled by AJAX/jQuery),
        // we let the AJAX prefilter handle it.
        if (event.defaultPrevented) return;

        const actionUrl = new URL(form.action || window.location.href, window.location.origin);
        if (actionUrl.origin !== window.location.origin) return; // Only intercept internal forms

        // ONLY intercept if we ALREADY know we are offline.
        // This is extremely fast (zero latency check) and does NOT block online form submits!
        if (!isServerReachable) {
            console.warn("Form submit intercepted offline. Saving transaction offline...", actionUrl.pathname);
            event.preventDefault();
            saveFormOffline(form);
        }
    });

    // 2. Listen for connection status changes
    window.addEventListener('online', syncOfflineData);
    
    // 3. Periodic sync check (every 30 seconds)
    setInterval(syncOfflineData, 30000);

    // 4. Initial sync check on page load
    if (navigator.onLine) {
        syncOfflineData();
    }

    // 5. Trigger caching of dynamic pages
    triggerPageCaching();
}

// Bind offline form interception and network monitoring
if (document.readyState === "loading") {
    document.addEventListener('DOMContentLoaded', initOfflineEngine);
} else {
    initOfflineEngine();
}

if ('serviceWorker' in navigator) {
    navigator.serviceWorker.addEventListener('controllerchange', triggerPageCaching);
}
*/