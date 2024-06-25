import {PrismaClient} from '@prisma/client'
import express from 'express'
import {getLatestClientRelease} from "./getLatestClientRelease";

const port = 3001;
const prisma = new PrismaClient()
const app = express()

app.use(express.json())

app.use(function(req, res, next) {
    res.header("Access-Control-Allow-Origin", "*");
    res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
    next();
});

app.post('/signUp', async (req, res) => {
    const { name, email, password } = req.body;

    const sameUser = await prisma.user.findFirst({ where: { OR: [ { email }, { name } ] } });

    if (sameUser) {
        res.status(303).send();
        return;
    }

    try {
        await prisma.user.create({
            data: {
                name,
                email,
                password
            },
        })
    } catch (e) {
        console.error(e);
    }

    res.status(200).send();
})

app.get('/client-version', async (req, res) => {
    const version = await getLatestClientRelease('Immortal-Vault', 'Client');
    return res.status(200).send({ version: version.replace('v', '') } );
})

app.get('/', (req, res) => {
    return res.send('Immortal Vault Server')
})

app.get('/ping', async (req, res) => {
    res.status(200).send('Pong!');
})

app.listen(port, () => console.log(`Server started on port ${port}`))

module.exports = app;
