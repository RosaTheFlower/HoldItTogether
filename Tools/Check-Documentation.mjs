import { readFileSync, readdirSync, existsSync, statSync } from 'node:fs';
import { resolve, dirname, relative, extname, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const docs = resolve(root, 'Docs');
const errors = [];
const ignored = new Set(['.git', '.vs', '.idea', 'bin', 'obj', 'artifacts', 'node_modules', 'TestResults', 'coverage']);
function walk(directory) {
    return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
        if (ignored.has(entry.name) || entry.isSymbolicLink()) return [];
        const path = resolve(directory, entry.name);
        return entry.isDirectory() ? walk(path) : [path];
    });
}
const markdown = walk(root).filter(path => extname(path).toLowerCase() === '.md');
const skillPath = /^\.agents\/skills\/([a-z0-9]+(?:-[a-z0-9]+)*)\/SKILL\.md$/;
const repositoryPath = path => relative(root, path).split(sep).join('/');
const instructions = markdown.filter(path => repositoryPath(path) === 'AGENTS.md' || skillPath.test(repositoryPath(path)));
const instructionPaths = new Set(instructions);
for (const path of markdown)
    if (!path.startsWith(docs + sep) && !instructionPaths.has(path))
        errors.push(`Documentation outside Docs/: ${relative(root, path)}`);
const guides = markdown.filter(path => path.startsWith(docs + sep));
let skills = 0;
for (const path of instructions) {
    const match = skillPath.exec(repositoryPath(path));
    if (!match) continue;
    skills++;
    // A deliberately restricted YAML subset; descriptions use JSON strings (valid YAML scalars).
    const content = readFileSync(path, 'utf8').replace(/\r\n/g, '\n');
    const frontmatter = /^---\nname: ([a-z0-9]+(?:-[a-z0-9]+)*)\ndescription: ("[^\n]+")\n---\n/.exec(content);
    if (!frontmatter) { errors.push(`${relative(root, path)}: expected name and JSON-quoted description frontmatter`); continue; }
    if (frontmatter[1] !== match[1] || frontmatter[1].length > 64)
        errors.push(`${relative(root, path)}: skill name must match its directory and contain at most 64 characters`);
    try {
        const description = JSON.parse(frontmatter[2]);
        if (!description.trim() || description.length > 1024 || /[<>]/.test(description))
            errors.push(`${relative(root, path)}: description must contain 1-1024 characters without angle brackets`);
    } catch { errors.push(`${relative(root, path)}: description must be a valid JSON-quoted string`); }
}
const withoutFences = text => text.replace(/^```[^\n]*\n[\s\S]*?^```\s*$/gm, '');
function anchors(path) {
    const seen = new Map();
    return new Set([...withoutFences(readFileSync(path, 'utf8')).matchAll(/^#{1,6}\s+(.+)$/gm)].map(match => {
        const base = match[1].trim().toLowerCase().replace(/<[^>]*>/g, '').replace(/[^\p{L}\p{N}_\-\s]/gu, '').replace(/\s/g, '-');
        const count = seen.get(base) ?? 0; seen.set(base, count + 1);
        return base + (count ? '-' + count : '');
    }));
}
let links = 0, jsonBlocks = 0;
for (const path of [...guides, ...instructions]) {
    const text = readFileSync(path, 'utf8');
    if (!/^# .+/m.test(text)) errors.push(`Missing title: ${relative(root, path)}`);
    for (const match of withoutFences(text).matchAll(/!?\[[^\]]*\]\(([^)]+)\)/g)) {
        const target = match[1].trim().replace(/^<|>$/g, '');
        if (/^[a-z][a-z0-9+.-]*:/i.test(target)) continue;
        const [file, fragment] = target.split('#');
        const destination = file ? resolve(dirname(path), decodeURIComponent(file)) : path;
        links++;
        if (!existsSync(destination)) { errors.push(`${relative(root, path)}: missing link ${target}`); continue; }
        if (fragment && extname(destination).toLowerCase() === '.md' && !anchors(destination).has(decodeURIComponent(fragment)))
            errors.push(`${relative(root, path)}: missing anchor ${target}`);
    }
    for (const block of text.matchAll(/^```json\s*\n([\s\S]*?)^```/gm)) {
        jsonBlocks++;
        try { JSON.parse(block[1]); } catch (error) { errors.push(`${relative(root, path)}: invalid JSON block: ${error.message}`); }
    }
}
const project = readFileSync(resolve(root, 'HoldItTogether.csproj'), 'utf8');
if (!/Content Include="Docs\/\*\*\/\*\.md"\s+CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest"/.test(project))
    errors.push('Main project must copy Docs/**/*.md to build and publish output.');
if (!existsSync(resolve(docs, 'README.md'))) errors.push('Missing Docs/README.md index.');
if (!guides.length) errors.push('No documentation found.');
if (process.argv.includes('--published')) {
    const index = process.argv.indexOf('--published');
    const output = process.argv[index + 1];
    if (!output) errors.push('--published requires a publication directory.');
    else for (const path of guides) {
        const published = resolve(root, output, relative(root, path));
        if (!existsSync(published) || !statSync(published).isFile()) errors.push(`Missing published guide: ${relative(root, path)}`);
        else if (readFileSync(path, 'utf8') !== readFileSync(published, 'utf8')) errors.push(`Stale published guide: ${relative(root, path)}`);
    }
}
if (errors.length) { for (const error of errors) console.error(error); process.exitCode = 1; }
else console.log(`PASS: ${guides.length} centralized guides, ${instructions.length} instruction files (${skills} skills), ${links} local links, ${jsonBlocks} JSON examples and documentation publication rules.`);
