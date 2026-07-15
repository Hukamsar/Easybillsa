/*
const CACHE_NAME = 'easybill-cache-v7';
const OFFLINE_URL = '/offline.html';

const ASSETS_TO_CACHE = [
    '/offline.html',
    '/css/site.css',
    '/js/site.js',
    '/css/adminlte.css',
    '/js/adminlte.min.js',
    '/Custom/global.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/lib/bootstrap/dist/css/bootstrap-icons.min.css',
    '/lib/bootstrap/dist/css/overlayscrollbars.min.css',
    '/lib/bootstrap/dist/css/jquery.dataTables.min.css',
    '/lib/bootstrap/dist/css/select2.min.css',
    '/bootstrap-select/css/bootstrap-select.css',
    '/favicon.ico',
    // External CDNs used in application layout
    'https://cdn.datatables.net/1.13.6/css/jquery.dataTables.min.css',
    'https://code.jquery.com/ui/1.13.2/themes/base/jquery-ui.css',
    'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css',
    'https://unpkg.com/intro.js/minified/introjs.min.css',
    'https://code.jquery.com/ui/1.13.2/jquery-ui.min.js',
    'https://cdn.datatables.net/1.13.6/js/jquery.dataTables.min.js',
    'https://unpkg.com/intro.js/minified/intro.min.js'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            // Pre-cache static assets atomically
            return cache.addAll(ASSETS_TO_CACHE).catch(err => {
                console.error('Pre-caching static assets failed:', err);
            });
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.map(key => {
                    if (key !== CACHE_NAME) {
                        return caches.delete(key);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    if (url.pathname === '/health') return; // Bypass health check from SW caching

    const isSameOrigin = url.origin === self.location.origin;
    if (!isSameOrigin) {
        if (event.request.method !== 'GET') return;
        const isCdn = url.hostname.includes("datatables.net") || 
                      url.hostname.includes("jquery.com") || 
                      url.hostname.includes("cloudflare.com") || 
                      url.hostname.includes("unpkg.com");
        if (!isCdn) return;
    }

    // Intercept POST requests when offline/unreachable to prevent page crashes (zero online latency)
    if (event.request.method === 'POST') {
        const isAjax = event.request.headers.get('X-Requested-With') === 'XMLHttpRequest' || 
                       (event.request.headers.get('accept') && event.request.headers.get('accept').includes('application/json'));

        event.respondWith(
            fetch(event.request).catch(async (err) => {
                console.warn("POST request failed in Service Worker. Saving offline...", url.pathname);
                
                let fields = {};
                let isAjaxRequest = isAjax;
                let contentType = event.request.headers.get('content-type') || '';
                
                try {
                    const reqClone = event.request.clone();
                    if (contentType.includes('json')) {
                        fields = await reqClone.json();
                    } else if (contentType.includes('multipart/form-data') || contentType.includes('form-urlencoded')) {
                        const formData = await reqClone.formData();
                        formData.forEach((value, key) => {
                            fields[key] = value;
                        });
                    } else {
                        const text = await reqClone.text();
                        fields = text;
                    }
                } catch (e) {
                    console.error("Failed to parse POST body in Service Worker:", e);
                }
                
                const formObj = {
                    action: url.pathname + url.search,
                    method: 'POST',
                    timestamp: Date.now(),
                    isAjax: isAjaxRequest,
                    contentType: contentType,
                    fields: fields
                };
                
                try {
                    const db = await getOfflineDBInSW();
                    const tx = db.transaction('offlineForms', 'readwrite');
                    tx.objectStore('offlineForms').add(formObj);
                    await new Promise((resolve, reject) => {
                        tx.oncomplete = resolve;
                        tx.onerror = reject;
                    });
                } catch (dbErr) {
                    console.error("Failed to save offline form in SW:", dbErr);
                }
                
                if (isAjaxRequest) {
                    return new Response("Offline", {
                        status: 503,
                        statusText: "Service Unavailable"
                    });
                } else {
                    const redirectUrl = event.request.referrer || url.pathname;
                    const htmlResponse = `
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <title>Offline Save</title>
                            <script>
                                localStorage.setItem('offline_save_detected', 'true');
                                window.location.href = "${redirectUrl}";
                            </script>
                        </head>
                        <body>
                            <p>Saving transaction offline, redirecting...</p>
                        </body>
                        </html>
                    `;
                    return new Response(htmlResponse, {
                        status: 200,
                        headers: { 'Content-Type': 'text/html' }
                    });
                }
            })
        );
        return;
    }

    if (event.request.method !== 'GET') return;

    // Handle HTML page requests (navigating or fetching HTML views): Network-First
    const isHtml = event.request.mode === 'navigate' || 
                   (event.request.headers.get('accept') && event.request.headers.get('accept').includes('text/html'));

    if (isHtml) {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    // Only cache successful, non-redirected authenticated pages
                    if (response.status === 200 && !response.redirected && 
                        !response.url.includes('/Login') && !response.url.includes('/Account/')) {
                        const responseClone = response.clone();
                        caches.open(CACHE_NAME).then(cache => {
                            cache.put(event.request, responseClone);
                        });
                    }
                    return response;
                })
                .catch(() => {
                    return caches.match(event.request).then(cachedResponse => {
                        if (cachedResponse) return cachedResponse;
                        return caches.match(OFFLINE_URL);
                    });
                })
        );
        return;
    }

    // Handle static assets and CDNs: Stale-While-Revalidate
    event.respondWith(
        caches.match(event.request).then(cachedResponse => {
            const fetchPromise = fetch(event.request)
                .then(networkResponse => {
                    if (networkResponse.status === 200 || networkResponse.status === 0) {
                        const responseClone = networkResponse.clone();
                        caches.open(CACHE_NAME).then(cache => {
                            cache.put(event.request, responseClone);
                        });
                    }
                    return networkResponse;
                })
                .catch(() => {
                    // Silently fail network requests if offline
                });
            return cachedResponse || fetchPromise;
        })
    );
});

// Listener for on-demand caching of authenticated pages from client
self.addEventListener('message', event => {
    if (event.data && event.data.action === 'cachePages') {
        caches.open(CACHE_NAME).then(cache => {
            event.data.pages.forEach(url => {
                fetch(url).then(response => {
                    if (response.status === 200 && !response.redirected && 
                        !response.url.includes('/Login') && !response.url.includes('/Account/')) {
                        cache.put(url, response);
                        console.log(`Successfully cached authenticated page: ${url}`);
                    } else {
                        console.warn(`Skipping cache for page (redirected/login): ${url}`);
                    }
                }).catch(err => {
                    console.warn(`Failed to fetch and cache page: ${url}`, err);
                });
            });
        });
    }
});

function getOfflineDBInSW() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open("EasyBillOfflineDB", 1);
        request.onupgradeneeded = e => {
            const db = e.target.result;
            if (!db.objectStoreNames.contains("offlineForms")) {
                db.createObjectStore("offlineForms", { keyPath: "id", autoIncrement: true });
            }
        };
        request.onsuccess = e => resolve(e.target.result);
        request.onerror = e => reject(e.target.error);
    });
}
*/
