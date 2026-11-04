// Validate a domain name
function isValidDomain(domain) {
    if (domain.length === 0 || domain.length > 253) {
        return false;
    }

    const labels = domain.split(".");

    return labels.every(label =>
        label.length >= 1 &&
        label.length <= 63 &&
        /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/.test(label)
    );
}


// Read and process the blocklist
async function loadBlocklist() {
    try {
        const response = await fetch(
            chrome.runtime.getURL("blocklist.txt")
        );

        if (!response.ok) {
            throw new Error(`HTTP error: ${response.status}`);
        }

        const text = await response.text();

        const domains = [];
        const seenDomains = new Set();

        let rejectedCount = 0;
        let duplicateCount = 0;

        const lines = text.split(/\r?\n/);

        for (const rawLine of lines) {
            let domain = rawLine.trim().toLowerCase();

            // Ignore comments and empty lines
            if (domain === "" || domain.startsWith("#")) {
                continue;
            }

            // Normalize wildcard entries
            if (domain.startsWith("*.")) {
                domain = domain.substring(2);
            }

            // Remove trailing dot
            domain = domain.replace(/\.$/, "");

            // Validate domain
            if (!isValidDomain(domain)) {
                rejectedCount++;
                console.warn("Invalid domain ignored:", rawLine);
                continue;
            }

            // Prevent duplicate entries
            if (seenDomains.has(domain)) {
                duplicateCount++;
                continue;
            }

            seenDomains.add(domain);
            domains.push(domain);
        }

        // Generate browser blocking rules
        const newRules = domains.map((domain, index) => ({
            id: index + 1,
            priority: 1,
            action: {
                type: "block"
            },
            condition: {
                urlFilter: `||${domain}^`,
                resourceTypes: ["main_frame"]
            }
        }));

        // Retrieve existing dynamic rules
        const existingRules =
            await chrome.declarativeNetRequest.getDynamicRules();

        // Replace existing rules
        await chrome.declarativeNetRequest.updateDynamicRules({
            removeRuleIds: existingRules.map(rule => rule.id),
            addRules: newRules
        });

        console.log("Blocker update complete.");
        console.log("Loaded domains:", domains.length);
        console.log("Rejected entries:", rejectedCount);
        console.log("Duplicates skipped:", duplicateCount);

    } catch (error) {
        console.error("Blocklist loading failed:", error);
    }
}


// Load blocklist after installation or update
chrome.runtime.onInstalled.addListener(loadBlocklist);

// Reload blocklist when browser starts
chrome.runtime.onStartup.addListener(loadBlocklist);