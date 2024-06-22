import { PrismaClient } from '@prisma/client'
import express from 'express'

const prisma = new PrismaClient()
const app = express()

app.use(express.json())

app.post('/init', async (req, res) => {
    await prisma.user.create({
        data: {
            name: 'admin',
            email: 'admin@gmail.com',
            password: 'test'
        },
    })

    const allUsers = await prisma.user.findMany({});
    res.status(200).json(allUsers);
})

app.get('/ping', async (req, res) => {
    res.status(200).send('Pong!');
})

app.listen(3001, () => console.log(`🚀 Server ready at: http://localhost:3000`))
